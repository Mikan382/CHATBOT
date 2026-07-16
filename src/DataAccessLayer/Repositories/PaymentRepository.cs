using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        _db.PaymentTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentTransaction?> GetByTxnRefAsync(string providerTxnRef, CancellationToken cancellationToken)
    {
        return await _db.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProviderTxnRef == providerTxnRef, cancellationToken);
    }

    public async Task<PaymentDashboardSummary> GetDashboardSummaryAsync(
        DateTime monthStartUtc,
        DateTime pendingSinceUtc,
        CancellationToken cancellationToken)
    {
        var paid = _db.PaymentTransactions.Where(x => x.Status == PaymentStatusNames.Paid);
        var paidThisMonth = paid.Where(x => x.PaidAtUtc >= monthStartUtc);
        return new PaymentDashboardSummary(
            await paidThisMonth.CountAsync(cancellationToken),
            await _db.PaymentTransactions.CountAsync(
                x => x.Status == PaymentStatusNames.Failed && x.UpdatedAtUtc >= monthStartUtc,
                cancellationToken),
            await _db.PaymentTransactions.CountAsync(
                x => x.Status == PaymentStatusNames.Pending && x.CreatedAtUtc >= pendingSinceUtc,
                cancellationToken),
            await paidThisMonth.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            await paid.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> ListRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        return await _db.PaymentTransactions
            .Include(x => x.Student)
            .Include(x => x.Plan)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkFailedAsync(Guid id, string? responseCode, string? rawResponse, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var affected = await _db.PaymentTransactions
            .Where(x => x.Id == id && x.Status == PaymentStatusNames.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, PaymentStatusNames.Failed)
                .SetProperty(x => x.ResponseCode, responseCode)
                .SetProperty(x => x.RawResponse, rawResponse)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        return affected > 0;
    }

    public async Task<bool> ConfirmPaidAndActivateAsync(
        Guid id,
        Guid newSubscriptionId,
        DateTime startedAtUtc,
        DateTime? expiresAtUtc,
        string? responseCode,
        string? providerTransactionNo,
        string? rawResponse,
        CancellationToken cancellationToken)
    {
        // Immutable fields needed to build the subscription. Reading them up front also gives a
        // cheap idempotency fast-path before we take a transaction/lock.
        var info = await _db.PaymentTransactions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.StudentUserId,
                x.SubscriptionPlanId,
                x.Amount,
                x.MessageQuota,
                x.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (info is null || info.Status != PaymentStatusNames.Pending)
        {
            return false;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // The gate: only the first concurrent callback flips Pending -> Paid and wins the right
        // to activate. A second return/IPN sees 0 rows and bails out without double-activating.
        var claimed = await _db.PaymentTransactions
            .Where(x => x.Id == id && x.Status == PaymentStatusNames.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, PaymentStatusNames.Paid)
                .SetProperty(x => x.PaidAtUtc, startedAtUtc)
                .SetProperty(x => x.ResponseCode, responseCode)
                .SetProperty(x => x.ProviderTransactionNo, providerTransactionNo)
                .SetProperty(x => x.RawResponse, rawResponse)
                .SetProperty(x => x.StudentSubscriptionId, newSubscriptionId)
                .SetProperty(x => x.UpdatedAtUtc, startedAtUtc), cancellationToken);
        if (claimed == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        // Only one Active subscription per student is allowed (filtered unique index), so retire
        // the current active package before inserting the newly paid one.
        var activeSubscriptions = await _db.StudentSubscriptions
            .Where(x => x.StudentUserId == info.StudentUserId && x.Status == SubscriptionStatusNames.Active)
            .ToListAsync(cancellationToken);
        foreach (var activeSubscription in activeSubscriptions)
        {
            activeSubscription.Status = activeSubscription.ExpiresAtUtc.HasValue
                && activeSubscription.ExpiresAtUtc <= startedAtUtc
                    ? SubscriptionStatusNames.Expired
                    : SubscriptionStatusNames.Replaced;
            activeSubscription.UpdatedAtUtc = startedAtUtc;
        }

        _db.StudentSubscriptions.Add(new StudentSubscription
        {
            Id = newSubscriptionId,
            StudentUserId = info.StudentUserId,
            SubscriptionPlanId = info.SubscriptionPlanId,
            Status = SubscriptionStatusNames.Active,
            PriceAtActivation = info.Amount,
            MessageQuotaAtActivation = info.MessageQuota,
            StartedAtUtc = startedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = startedAtUtc,
            UpdatedAtUtc = startedAtUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
