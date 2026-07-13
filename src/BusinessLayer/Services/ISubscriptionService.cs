namespace BusinessLayer.Services;

public interface ISubscriptionService
{
    Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken);
    Task SubscribeAsync(Guid studentUserId, Guid planId, CancellationToken cancellationToken);
    Task CancelCurrentAsync(Guid studentUserId, CancellationToken cancellationToken);
    Task<SubscriptionDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task CreatePlanAsync(
        string code,
        string name,
        string? description,
        decimal monthlyPrice,
        int durationDays,
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
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken);
}
