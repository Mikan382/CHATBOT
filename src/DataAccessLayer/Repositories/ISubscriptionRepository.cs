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
    Task ActivateFreePlanAsync(StudentSubscription subscription, DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> CountStudentsAsync(CancellationToken cancellationToken);
    Task<int> CountActiveSubscriptionsAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> CountActivationsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionPlanCount>> CountActiveByPlanAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<ActiveChatUsageSummary> GetActiveChatUsageAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentSubscription>> ListRecentSubscriptionsAsync(int take, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public record ActiveChatUsageSummary(int UsedMessages, int FiniteQuota, int UnlimitedSubscriptions);
