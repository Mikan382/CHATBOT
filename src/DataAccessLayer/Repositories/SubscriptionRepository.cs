using Microsoft.EntityFrameworkCore;
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
            .ThenBy(x => x.MonthlyPrice)
            .ThenBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetPlanAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
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
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _db.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public async Task<StudentSubscription?> GetForDecisionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        return await _db.StudentSubscriptions
            .Include(x => x.Student)
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);
    }

    public async Task<int> CountStudentsAsync(CancellationToken cancellationToken)
    {
        return await _db.Users.CountAsync(x => x.Role == UserRoleNames.Student && !x.IsLockedOut, cancellationToken);
    }

    public async Task<int> CountActiveSubscriptionsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await ActiveSubscriptions(nowUtc).CountAsync(cancellationToken);
    }

    public async Task<int> CountRequestsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken)
    {
        return await _db.StudentSubscriptions.CountAsync(x => x.CreatedAtUtc >= sinceUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPlanCount>> CountActiveByPlanAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await ActiveSubscriptions(nowUtc)
            .GroupBy(x => x.SubscriptionPlanId)
            .Select(x => new SubscriptionPlanCount(x.Key, x.Count(), x.Sum(s => s.PriceAtActivation)))
            .ToListAsync(cancellationToken);
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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<StudentSubscription> ActiveSubscriptions(DateTime nowUtc)
    {
        return _db.StudentSubscriptions
            .Where(x => x.Status == SubscriptionStatusNames.Active
                && (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > nowUtc));
    }
}
