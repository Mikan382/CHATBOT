using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface ISubscriptionService
{
    Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken);
    Task ActivateFreePlanAsync(Guid studentUserId, Guid planId, CancellationToken cancellationToken);
    Task RevokeSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);
    Task<SubscriptionDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task CreatePlanAsync(
        string code,
        string name,
        string? description,
        decimal monthlyPrice,
        int durationDays,
        int messageQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken);
    Task UpdatePlanAsync(
        Guid id,
        string code,
        string name,
        string? description,
        decimal monthlyPrice,
        int durationDays,
        int messageQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken);
}
