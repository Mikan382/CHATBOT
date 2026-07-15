using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SubscriptionService(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: false, cancellationToken);
        var current = await _subscriptionRepository.GetCurrentForStudentAsync(studentUserId, DateTime.UtcNow, cancellationToken);
        return new StudentSubscriptionPageDto(
            ToStudentSubscriptionDto(current),
            plans.Select(ToPlanDto).ToList());
    }

    public async Task RevokeSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetForDecisionAsync(subscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription was not found.");

        if (subscription.Status != SubscriptionStatusNames.Active)
        {
            throw new InvalidOperationException("Only an active subscription can be revoked.");
        }

        subscription.Status = SubscriptionStatusNames.Cancelled;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubscriptionDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: true, cancellationToken);
        var counts = await _subscriptionRepository.CountActiveByPlanAsync(now, cancellationToken);
        var statsByPlan = counts.ToDictionary(x => x.PlanId);
        var recent = await _subscriptionRepository.ListRecentSubscriptionsAsync(12, cancellationToken);
        var planStats = plans.Select(plan =>
        {
            var stats = statsByPlan.GetValueOrDefault(plan.Id);
            return new SubscriptionPlanStatsDto(
                plan.Id,
                plan.Name,
                plan.Code,
                plan.IsActive,
                stats?.Count ?? 0,
                stats?.EstimatedValue ?? 0m);
        }).ToList();

        return new SubscriptionDashboardDto(
            await _subscriptionRepository.CountStudentsAsync(cancellationToken),
            await _subscriptionRepository.CountActiveSubscriptionsAsync(now, cancellationToken),
            await _subscriptionRepository.CountRequestsSinceAsync(monthStart, cancellationToken),
            planStats.Sum(x => x.EstimatedMonthlyRevenue),
            planStats,
            recent.Select(x => ToRecentDto(x, now)).ToList(),
            plans.Select(ToPlanDto).ToList());
    }

    public async Task CreatePlanAsync(
        string code,
        string name,
        string? description,
        decimal monthlyPrice,
        int durationDays,
        int messageQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        ValidatePlan(monthlyPrice, durationDays, messageQuota);

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
            MessageQuota = messageQuota,
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
        int messageQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        ValidatePlan(monthlyPrice, durationDays, messageQuota);

        if (await _subscriptionRepository.PlanCodeExistsAsync(code, id, cancellationToken))
        {
            throw new InvalidOperationException("Subscription package code already exists.");
        }

        plan.Code = code;
        plan.Name = name;
        plan.Description = description?.Trim() ?? "";
        plan.MonthlyPrice = monthlyPrice;
        plan.DurationDays = durationDays;
        plan.MessageQuota = messageQuota;
        plan.SortOrder = sortOrder;
        plan.IsActive = isActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePlan(decimal monthlyPrice, int durationDays, int messageQuota)
    {
        if (monthlyPrice < 0)
        {
            throw new InvalidOperationException("Monthly price cannot be negative.");
        }

        if (durationDays < 0)
        {
            throw new InvalidOperationException("Duration days cannot be negative.");
        }

        if (messageQuota < 0)
        {
            throw new InvalidOperationException("Message quota cannot be negative.");
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
            plan.MessageQuota,
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

    private static RecentSubscriptionDto ToRecentDto(StudentSubscription subscription, DateTime nowUtc)
    {
        var status = subscription.Status == SubscriptionStatusNames.Active
            && subscription.ExpiresAtUtc.HasValue
            && subscription.ExpiresAtUtc <= nowUtc
                ? SubscriptionStatusNames.Expired
                : subscription.Status;
        return new RecentSubscriptionDto(
            subscription.Id,
            subscription.Student?.Email ?? "",
            subscription.Student?.DisplayName ?? "",
            subscription.Plan?.Name ?? "",
            status,
            subscription.CreatedAtUtc,
            subscription.ExpiresAtUtc);
    }
}
