using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IPaymentRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken);
    Task<PaymentTransaction?> GetByTxnRefAsync(string providerTxnRef, CancellationToken cancellationToken);

    // Pending -> Failed. Returns false if the row was already finalised (idempotent no-op).
    Task<bool> MarkFailedAsync(Guid id, string? responseCode, string? rawResponse, CancellationToken cancellationToken);

    // Atomically flips a Pending transaction to Paid and activates a fresh Active subscription
    // (replacing any current Active one) in a single DB transaction. Returns false when another
    // callback already finalised this transaction, so double return/IPN calls never double-activate.
    Task<bool> ConfirmPaidAndActivateAsync(
        Guid id,
        Guid newSubscriptionId,
        DateTime startedAtUtc,
        DateTime? expiresAtUtc,
        string? responseCode,
        string? providerTransactionNo,
        string? rawResponse,
        CancellationToken cancellationToken);
}
