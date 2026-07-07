using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.Helpers;

namespace BusinessLayer.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserAdminRepository _userRepository;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IUserAdminRepository userRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
    }

    public async Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: false, cancellationToken);
        var current = await _subscriptionRepository.GetCurrentForStudentAsync(studentUserId, cancellationToken);
        return new StudentSubscriptionPageDto(
            ToStudentSubscriptionDto(current),
            plans.Select(ToPlanDto).ToList());
    }

    public async Task SubscribeAsync(Guid studentUserId, Guid planId, CancellationToken cancellationToken)
    {
        var student = await _userRepository.GetByIdAsync(studentUserId, cancellationToken)
            ?? throw new InvalidOperationException("Student account was not found.");
        if (student.Role != UserRoleNames.Student)
        {
            throw new InvalidOperationException("Only students can register for a subscription package.");
        }

        var plan = await _subscriptionRepository.GetPlanAsync(planId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");
        if (!plan.IsActive)
        {
            throw new InvalidOperationException("This subscription package is not active.");
        }

        var now = DateTime.UtcNow;
        var current = await _subscriptionRepository.GetCurrentForStudentAsync(studentUserId, cancellationToken);
        if (current is not null)
        {
            if (current.SubscriptionPlanId == planId && (!current.ExpiresAtUtc.HasValue || current.ExpiresAtUtc > now))
            {
                throw new InvalidOperationException("You are already registered for this package.");
            }

            current.Status = SubscriptionStatusNames.Replaced;
            current.UpdatedAtUtc = now;
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);
        }

        await _subscriptionRepository.AddSubscriptionAsync(new StudentSubscription
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            SubscriptionPlanId = planId,
            Status = SubscriptionStatusNames.Active,
            StartedAtUtc = now,
            ExpiresAtUtc = plan.DurationDays > 0 ? now.AddDays(plan.DurationDays) : null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);
    }

    public async Task CancelCurrentAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var current = await _subscriptionRepository.GetCurrentForStudentAsync(studentUserId, cancellationToken)
            ?? throw new InvalidOperationException("You do not have an active subscription.");

        current.Status = SubscriptionStatusNames.Cancelled;
        current.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubscriptionDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: true, cancellationToken);
        var counts = await _subscriptionRepository.CountActiveByPlanAsync(now, cancellationToken);
        var countByPlan = counts.ToDictionary(x => x.PlanId, x => x.Count);
        var recent = await _subscriptionRepository.ListRecentSubscriptionsAsync(12, cancellationToken);

        return new SubscriptionDashboardDto(
            await _subscriptionRepository.CountStudentsAsync(cancellationToken),
            await _subscriptionRepository.CountActiveSubscriptionsAsync(now, cancellationToken),
            await _subscriptionRepository.CountNewSubscriptionsSinceAsync(monthStart, cancellationToken),
            plans.Select(plan => new SubscriptionPlanStatsDto(
                plan.Id,
                plan.Name,
                plan.Code,
                plan.IsActive,
                countByPlan.GetValueOrDefault(plan.Id, 0))).ToList(),
            recent.Select(ToRecentDto).ToList(),
            plans.Select(ToPlanDto).ToList());
    }

    public async Task CreatePlanAsync(
        string code,
        string name,
        string? description,
        decimal monthlyPrice,
        int durationDays,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        ValidatePlan(monthlyPrice, durationDays);

        if (await _subscriptionRepository.PlanCodeExistsAsync(code, null, cancellationToken))
        {
            throw new InvalidOperationException("Subscription package code already exists.");
        }

        var now = DateTime.UtcNow;
        await _subscriptionRepository.AddPlanAsync(new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description?.Trim() ?? "",
            MonthlyPrice = monthlyPrice,
            DurationDays = durationDays,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);
    }

    public async Task UpdatePlanAsync(
        Guid id,
        string code,
        string name,
        string? description,
        decimal monthlyPrice,
        int durationDays,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        ValidatePlan(monthlyPrice, durationDays);

        if (await _subscriptionRepository.PlanCodeExistsAsync(code, id, cancellationToken))
        {
            throw new InvalidOperationException("Subscription package code already exists.");
        }

        plan.Code = code;
        plan.Name = name;
        plan.Description = description?.Trim() ?? "";
        plan.MonthlyPrice = monthlyPrice;
        plan.DurationDays = durationDays;
        plan.SortOrder = sortOrder;
        plan.IsActive = isActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPlanActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        plan.IsActive = isActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePlan(decimal monthlyPrice, int durationDays)
    {
        if (monthlyPrice < 0)
        {
            throw new InvalidOperationException("Monthly price cannot be negative.");
        }

        if (durationDays < 0)
        {
            throw new InvalidOperationException("Duration days cannot be negative.");
        }
    }

    private static string NormalizeCode(string code)
    {
        return StringHelper.NormalizeRequired(code, "Package code").ToUpperInvariant();
    }

    private static SubscriptionPlanDto ToPlanDto(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.DurationDays,
            plan.SortOrder,
            plan.IsActive);
    }

    private static StudentSubscriptionDto? ToStudentSubscriptionDto(StudentSubscription? subscription)
    {
        if (subscription?.Plan is null)
        {
            return null;
        }

        return new StudentSubscriptionDto(
            subscription.Id,
            subscription.Plan.Id,
            subscription.Plan.Name,
            subscription.Plan.Code,
            subscription.Status,
            subscription.StartedAtUtc,
            subscription.ExpiresAtUtc);
    }

    private static RecentSubscriptionDto ToRecentDto(StudentSubscription subscription)
    {
        return new RecentSubscriptionDto(
            subscription.Id,
            subscription.Student?.Email ?? "",
            subscription.Student?.DisplayName ?? "",
            subscription.Plan?.Name ?? "",
            subscription.Status,
            subscription.StartedAtUtc,
            subscription.ExpiresAtUtc);
    }
}
