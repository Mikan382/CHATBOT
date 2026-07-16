using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Payment;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway)
    {
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
    }

    public async Task ActivateFreePlanAsync(
        Guid studentUserId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(planId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");
        if (!plan.IsActive || plan.MonthlyPrice != 0)
        {
            throw new InvalidOperationException("Only an active free package can be activated without payment.");
        }

        var now = DateTime.UtcNow;
        var current = await _subscriptionRepository.GetCurrentForStudentAsync(studentUserId, now, cancellationToken);
        if (current is not null)
        {
            throw new InvalidOperationException("Your current package must expire or be revoked before activating a free package.");
        }

        await _subscriptionRepository.ActivateFreePlanAsync(new StudentSubscription
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatusNames.Active,
            PriceAtActivation = 0,
            MessageQuotaAtActivation = plan.MessageQuota,
            StartedAtUtc = now,
            ExpiresAtUtc = plan.DurationDays > 0 ? now.AddDays(plan.DurationDays) : null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, now, cancellationToken);
    }

    public async Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: false, cancellationToken);
        var current = await _subscriptionRepository.GetCurrentForStudentAsync(studentUserId, DateTime.UtcNow, cancellationToken);
        return new StudentSubscriptionPageDto(
            ToStudentSubscriptionDto(current),
            plans.Select(ToPlanDto).ToList(),
            _paymentGateway.IsConfigured);
    }

    public async Task RevokeSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetForDecisionAsync(subscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription was not found.");

        if (subscription.Status != SubscriptionStatusNames.Active)
        {
            throw new InvalidOperationException("Only an active subscription can be revoked.");
        }

        if (subscription.ExpiresAtUtc.HasValue && subscription.ExpiresAtUtc <= DateTime.UtcNow)
        {
            subscription.Status = SubscriptionStatusNames.Expired;
            subscription.UpdatedAtUtc = DateTime.UtcNow;
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This subscription has already expired.");
        }

        subscription.Status = SubscriptionStatusNames.Cancelled;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubscriptionDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var pendingSince = now.Subtract(_paymentGateway.CheckoutLifetime);
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: true, cancellationToken);
        var counts = await _subscriptionRepository.CountActiveByPlanAsync(now, cancellationToken);
        var payments = await _paymentRepository.GetDashboardSummaryAsync(monthStart, pendingSince, cancellationToken);
        var chatUsage = await _subscriptionRepository.GetActiveChatUsageAsync(now, cancellationToken);
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
                stats?.ActiveValue ?? 0m);
        }).ToList();

        return new SubscriptionDashboardDto(
            await _subscriptionRepository.CountStudentsAsync(cancellationToken),
            await _subscriptionRepository.CountActiveSubscriptionsAsync(now, cancellationToken),
            await _subscriptionRepository.CountActivationsSinceAsync(monthStart, cancellationToken),
            payments.PaidThisMonth,
            payments.FailedThisMonth,
            payments.Pending,
            payments.RevenueThisMonth,
            payments.TotalRevenue,
            planStats.Sum(x => x.ActivePackageValue),
            chatUsage.UsedMessages,
            chatUsage.FiniteQuota,
            chatUsage.UnlimitedSubscriptions,
            planStats,
            recent.Select(x => ToRecentDto(x, now)).ToList(),
            (await _paymentRepository.ListRecentAsync(12, cancellationToken))
                .Select(x => ToRecentPaymentDto(x, pendingSince))
                .ToList(),
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

        if (monthlyPrice != decimal.Truncate(monthlyPrice))
        {
            throw new InvalidOperationException("Package price must be a whole VND amount.");
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
            subscription.MessageQuotaAtActivation,
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
            subscription.StartedAtUtc,
            subscription.ExpiresAtUtc);
    }

    private static RecentPaymentDto ToRecentPaymentDto(PaymentTransaction payment, DateTime pendingSinceUtc)
    {
        var status = payment.Status == PaymentStatusNames.Pending && payment.CreatedAtUtc < pendingSinceUtc
            ? PaymentStatusNames.Expired
            : payment.Status;
        return new RecentPaymentDto(
            payment.Id,
            payment.Student?.Email ?? "",
            payment.Student?.DisplayName ?? "",
            payment.Plan?.Name ?? "",
            payment.Amount,
            status,
            payment.PaidAtUtc ?? payment.UpdatedAtUtc);
    }
}
