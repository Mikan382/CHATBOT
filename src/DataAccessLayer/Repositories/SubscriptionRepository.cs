using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _db;

    public SubscriptionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubscriptionPlan>> ListPlansAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _db.SubscriptionPlans.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .ThenBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetPlanAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetDefaultPlanAsync(CancellationToken cancellationToken)
    {
        return await _db.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IsDefault && x.IsActive && x.Price == 0,
                cancellationToken);
    }

    public async Task<bool> PlanCodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _db.SubscriptionPlans.AnyAsync(
            x => x.Code == normalizedCode && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        if (!plan.IsDefault)
        {
            _db.SubscriptionPlans.Add(plan);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await ClearOtherDefaultsAsync(plan.Id, cancellationToken);
        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SavePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        if (!plan.IsDefault)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await ClearOtherDefaultsAsync(plan.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<StudentSubscription?> GetCurrentForStudentAsync(
        Guid studentUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await _db.StudentSubscriptions
            .Include(x => x.Plan)
            .Where(x => x.StudentUserId == studentUserId
                && x.Status == SubscriptionStatusNames.Active
                && (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > nowUtc))
            .OrderByDescending(x => x.StartedAtUtc)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StudentSubscription?> GetOrCreateCurrentForStudentAsync(
        Guid studentUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var current = await GetCurrentForStudentAsync(studentUserId, nowUtc, cancellationToken);
        if (current is not null)
        {
            return current;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var activeRows = await _db.StudentSubscriptions
            .Include(x => x.Plan)
            .Where(x => x.StudentUserId == studentUserId
                && x.Status == SubscriptionStatusNames.Active)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
        current = activeRows.FirstOrDefault(
            x => !x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > nowUtc);
        if (current is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return current;
        }

        foreach (var expired in activeRows)
        {
            expired.Status = SubscriptionStatusNames.Expired;
            expired.UpdatedAtUtc = nowUtc;
        }

        var defaultPlan = await _db.SubscriptionPlans
            .FirstOrDefaultAsync(
                x => x.IsDefault && x.IsActive && x.Price == 0,
                cancellationToken);
        if (defaultPlan is null)
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var subscription = new StudentSubscription
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            SubscriptionPlanId = defaultPlan.Id,
            Plan = defaultPlan,
            Status = SubscriptionStatusNames.Active,
            PriceAtActivation = defaultPlan.Price,
            TokenQuotaAtActivation = defaultPlan.TokenQuota,
            StartedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddDays(defaultPlan.DurationDays),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        _db.StudentSubscriptions.Add(subscription);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return subscription;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return await GetCurrentForStudentAsync(studentUserId, nowUtc, cancellationToken);
        }
    }

    public async Task<int> CountStudentsAsync(CancellationToken cancellationToken)
    {
        return await _db.Users.CountAsync(x => x.Role == UserRoleNames.Student && !x.IsLockedOut, cancellationToken);
    }

    public async Task<int> CountActiveSubscriptionsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await ActiveSubscriptions(nowUtc).CountAsync(cancellationToken);
    }

    public async Task<int> CountActivationsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken)
    {
        return await _db.StudentSubscriptions.CountAsync(x => x.StartedAtUtc >= sinceUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPlanCount>> CountActiveByPlanAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await ActiveSubscriptions(nowUtc)
            .GroupBy(x => x.SubscriptionPlanId)
            .Select(x => new SubscriptionPlanCount(x.Key, x.Count(), x.Sum(s => s.PriceAtActivation)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActiveTokenUsageSummary> GetActiveTokenUsageAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await ActiveSubscriptions(nowUtc)
            .GroupBy(_ => 1)
            .Select(group => new ActiveTokenUsageSummary(
                group.Sum(x => x.InputTokensUsed),
                group.Sum(x => x.OutputTokensUsed),
                group.Sum(x => x.TotalTokensUsed),
                group.Sum(x => x.TokenQuotaAtActivation)))
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ActiveTokenUsageSummary(0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<StudentSubscription>> ListRecentSubscriptionsAsync(int take, CancellationToken cancellationToken)
    {
        return await _db.StudentSubscriptions
            .Include(x => x.Student)
            .Include(x => x.Plan)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task ClearOtherDefaultsAsync(Guid planId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await _db.SubscriptionPlans
            .Where(x => x.Id != planId && x.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDefault, false)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }

    private IQueryable<StudentSubscription> ActiveSubscriptions(DateTime nowUtc)
    {
        return _db.StudentSubscriptions
            .Where(x => x.Status == SubscriptionStatusNames.Active
                && (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > nowUtc));
    }
}
