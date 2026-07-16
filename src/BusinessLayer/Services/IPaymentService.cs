namespace BusinessLayer.Services;

// Success drives the student-facing flash; the Ipn* fields are the JSON body VNPay expects on its
// server-to-server IPN call (RspCode "00" = acknowledged).
public record PaymentConfirmResult(bool Success, string Message, string IpnResponseCode, string IpnMessage);

public interface IPaymentService
{
    // Creates a Pending payment transaction and returns the gateway redirect URL. No subscription
    // is created yet; activation happens only after a verified payment callback.
    Task<string> CreateCheckoutAsync(
        Guid studentUserId,
        Guid planId,
        string ipAddress,
        string returnUrl,
        CancellationToken cancellationToken);

    // Verifies and processes a return/IPN callback. Idempotent: safe to call for both the browser
    // return and the server IPN, and for retries of either.
    Task<PaymentConfirmResult> ConfirmAsync(IReadOnlyDictionary<string, string> callback, CancellationToken cancellationToken);
}
