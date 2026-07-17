using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface ISubscriptionService
{
    Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken);
    Task<SubscriptionDashboardDto> GetDashboardAsync(int periodDays, CancellationToken cancellationToken);
    Task<IReadOnlyList<ActiveSubscriptionDetailDto>> GetActiveSubscriptionDetailsAsync(CancellationToken cancellationToken);
    Task CreatePlanAsync(
        string code,
        string name,
        string? description,
        decimal price,
        int durationDays,
        long tokenQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken);
    Task UpdatePlanAsync(
        Guid id,
        string code,
        string name,
        string? description,
        decimal price,
        int durationDays,
        long tokenQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken);
    Task SetDefaultPlanAsync(Guid planId, CancellationToken cancellationToken);
}
