using System.Text;
using BusinessLayer.Payment;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class PaymentService : IPaymentService
{
    private const string SuccessResponseCode = "00";
    private readonly IPaymentGateway _gateway;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserAdminRepository _userRepository;

    public PaymentService(
        IPaymentGateway gateway,
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IUserAdminRepository userRepository)
    {
        _gateway = gateway;
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
    }

    public async Task<string> CreateCheckoutAsync(
        Guid studentUserId,
        Guid planId,
        string ipAddress,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        if (!_gateway.IsConfigured)
        {
            throw new InvalidOperationException("Online payment is not configured. Please contact the administrator.");
        }

        var student = await _userRepository.GetByIdAsync(studentUserId, cancellationToken)
            ?? throw new InvalidOperationException("Student account was not found.");
        if (student.Role != UserRoleNames.Student || student.IsLockedOut)
        {
            throw new InvalidOperationException("This account is not eligible to purchase a subscription package.");
        }

        var plan = await _subscriptionRepository.GetPlanAsync(planId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");
        if (!plan.IsActive)
        {
            throw new InvalidOperationException("This subscription package is not active.");
        }

        if (plan.Price <= 0)
        {
            throw new InvalidOperationException("This package is free and does not require payment.");
        }

        if (plan.Price != decimal.Truncate(plan.Price))
        {
            throw new InvalidOperationException("VNPay packages must use a whole VND amount.");
        }

        var now = DateTime.UtcNow;
        var current = await _subscriptionRepository.GetOrCreateCurrentForStudentAsync(
            studentUserId,
            now,
            cancellationToken);
        if (current?.Plan is null)
        {
            throw new InvalidOperationException(
                "No default package is configured. Please contact the administrator.");
        }

        if (current.PriceAtActivation > 0)
        {
            throw new InvalidOperationException(
                "Your current paid package must expire before you can purchase another package.");
        }

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            SubscriptionPlanId = planId,
            Provider = _gateway.ProviderName,
            Amount = plan.Price,
            DurationDays = plan.DurationDays,
            TokenQuota = plan.TokenQuota,
            Status = PaymentStatusNames.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        transaction.ProviderTxnRef = transaction.Id.ToString("N");
        var pendingCreated = await _paymentRepository.TryAddPendingAsync(
            transaction,
            now.Subtract(_gateway.CheckoutLifetime),
            cancellationToken);
        if (!pendingCreated)
        {
            throw new InvalidOperationException(
                "A VNPay checkout is already pending. Complete it or wait for it to expire before trying again.");
        }

        var checkoutExpiresAtUtc = now.Add(_gateway.CheckoutLifetime);

        return _gateway.BuildCheckoutUrl(new PaymentCheckoutRequest(
            transaction.ProviderTxnRef,
            decimal.ToInt64(plan.Price),
            $"Thanh toan goi {plan.Code} {transaction.ProviderTxnRef}",
            ipAddress,
            returnUrl,
            now,
            checkoutExpiresAtUtc));
    }

    public async Task<PaymentConfirmResult> ConfirmAsync(IReadOnlyDictionary<string, string> callback, CancellationToken cancellationToken)
    {
        var result = _gateway.VerifyCallback(callback);
        if (!result.SignatureValid)
        {
            return new PaymentConfirmResult(false, "Invalid payment signature.", "97", "Invalid signature");
        }

        var transaction = await _paymentRepository.GetByTxnRefAsync(result.TxnRef, cancellationToken);
        if (transaction is null)
        {
            return new PaymentConfirmResult(false, "Payment transaction was not found.", "01", "Order not found");
        }

        var expectedAmount = decimal.ToInt64(transaction.Amount) * 100;
        if (result.Amount != expectedAmount)
        {
            return new PaymentConfirmResult(false, "Payment amount does not match.", "04", "Invalid amount");
        }

        if (transaction.Status == PaymentStatusNames.Paid)
        {
            return new PaymentConfirmResult(true, "This payment was already confirmed.", "02", "Order already confirmed");
        }

        if (transaction.Status == PaymentStatusNames.Failed)
        {
            return new PaymentConfirmResult(false, "This payment was already marked as failed.", "02", "Order already confirmed");
        }

        if (transaction.Status == PaymentStatusNames.Expired)
        {
            return new PaymentConfirmResult(false, "This payment checkout has expired.", "02", "Order already confirmed");
        }

        var raw = SerializeCallback(callback);
        var paySucceeded = result.ResponseCode == SuccessResponseCode
            && result.TransactionStatus == SuccessResponseCode;
        if (!paySucceeded)
        {
            var markedFailed = await _paymentRepository.MarkFailedAsync(
                transaction.Id,
                result.ResponseCode,
                raw,
                cancellationToken);
            if (!markedFailed)
            {
                var latest = await _paymentRepository.GetByTxnRefAsync(result.TxnRef, cancellationToken);
                if (latest?.Status == PaymentStatusNames.Paid)
                {
                    return new PaymentConfirmResult(true, "This payment was already confirmed.", "02", "Order already confirmed");
                }
            }

            return new PaymentConfirmResult(false, "Payment was not successful.", "00", "Confirm Success");
        }

        var now = DateTime.UtcNow;
        DateTime? expiresAt = transaction.DurationDays > 0 ? now.AddDays(transaction.DurationDays) : null;

        var activated = await _paymentRepository.ConfirmPaidAndActivateAsync(
            transaction.Id,
            Guid.NewGuid(),
            now,
            expiresAt,
            result.ResponseCode,
            result.TransactionNo,
            raw,
            cancellationToken);

        if (!activated)
        {
            var latest = await _paymentRepository.GetByTxnRefAsync(result.TxnRef, cancellationToken);
            return latest?.Status == PaymentStatusNames.Paid
                ? new PaymentConfirmResult(true, "This payment was already confirmed.", "02", "Order already confirmed")
                : new PaymentConfirmResult(false, "Payment confirmation could not be completed.", "99", "Unknown error");
        }

        return new PaymentConfirmResult(true, "Payment successful. Your package is now active.", "00", "Confirm Success");
    }

    private static string SerializeCallback(IReadOnlyDictionary<string, string> callback)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in callback.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (builder.Length > 0) builder.Append('&');
            builder.Append(Uri.EscapeDataString(key))
                .Append('=')
                .Append(Uri.EscapeDataString(value));
        }

        var raw = builder.ToString();
        return raw.Length <= 4000 ? raw : raw[..4000];
    }
}
