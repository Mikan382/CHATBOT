using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ISubscriptionRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> ListPlansAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<SubscriptionPlan?> GetPlanAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> PlanCodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken);
    Task<StudentSubscription?> GetCurrentForStudentAsync(Guid studentUserId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<StudentSubscription?> GetForDecisionAsync(Guid subscriptionId, CancellationToken cancellationToken);
    Task<SubscriptionPlan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken);
    Task<int> CountStudentsAsync(CancellationToken cancellationToken);
    Task<int> CountActiveSubscriptionsAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> CountRequestsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionPlanCount>> CountActiveByPlanAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentSubscription>> ListRecentSubscriptionsAsync(int take, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
