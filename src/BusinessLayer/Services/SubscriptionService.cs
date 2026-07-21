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

    public async Task<StudentSubscriptionPageDto> GetStudentPageAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: false, cancellationToken);
        var current = await _subscriptionRepository.GetOrCreateCurrentForStudentAsync(
            studentUserId,
            DateTime.UtcNow,
            cancellationToken);
        return new StudentSubscriptionPageDto(
            ToStudentSubscriptionDto(current),
            plans.Select(ToPlanDto).ToList(),
            _paymentGateway.IsConfigured);
    }

    public async Task<SubscriptionDashboardDto> GetDashboardAsync(int periodDays, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sinceUtc = periodDays > 0
            ? now.AddDays(-periodDays)
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var untilUtc = now;
        var pendingSince = now.Subtract(_paymentGateway.CheckoutLifetime);

        // Core data
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: true, cancellationToken);
        var activeSubscriptions = await _subscriptionRepository.ListActiveSubscriptionsAsync(now, cancellationToken);
        var activeCount = activeSubscriptions.Count;

        // Time-filtered KPIs
        var paidActivations = await _subscriptionRepository.CountPaidActivationsAsync(sinceUtc, untilUtc, cancellationToken);
        var payments = await _paymentRepository.GetDashboardSummaryForRangeAsync(sinceUtc, untilUtc, pendingSince, cancellationToken);

        // Token usage from active subscriptions
        long totalTokensUsed = activeSubscriptions.Sum(x => x.TotalTokensUsed);
        long totalTokenQuota = activeSubscriptions.Sum(x => x.TokenQuotaAtActivation);
        decimal tokenUtilization = totalTokenQuota > 0
            ? Math.Round((decimal)totalTokensUsed / totalTokenQuota * 100, 1)
            : 0m;

        // Trend data
        var dailyRevenue = await _paymentRepository.GetDailyRevenueAsync(sinceUtc, untilUtc, cancellationToken);
        var trend = dailyRevenue.Select(d =>
            new SubscriptionTrendPointDto(d.Date, d.Revenue, d.PaidActivations)).ToList();

        // Package performance
        var activeByPlan = activeSubscriptions
            .GroupBy(x => x.SubscriptionPlanId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get paid activations and revenue per plan for the time range
        var paidSubsInRange = await GetPaidSubscriptionsInRangeAsync(sinceUtc, untilUtc, cancellationToken);

        var packagePerformance = plans.Select(plan =>
        {
            var planActiveSubs = activeByPlan.GetValueOrDefault(plan.Id)
                ?? new List<StudentSubscription>();
            var planActiveUsers = planActiveSubs.Count;
            var planTokenUsed = planActiveSubs.Sum(s => s.TotalTokensUsed);
            var planTokenQuota = planActiveSubs.Sum(s => s.TokenQuotaAtActivation);
            decimal? utilization = planTokenQuota > 0
                ? Math.Round((decimal)planTokenUsed / planTokenQuota * 100, 1)
                : null;

            var planPaidActivations = paidSubsInRange.Count(s => s.SubscriptionPlanId == plan.Id);
            var planRevenue = paidSubsInRange
                .Where(s => s.SubscriptionPlanId == plan.Id)
                .Sum(s => s.PriceAtActivation);

            return new PackagePerformanceDto(
                plan.Id, plan.Code, plan.Name,
                planActiveUsers, planPaidActivations, planRevenue,
                planTokenUsed, planTokenQuota, utilization);
        }).ToList();

        // Needs attention
        var expiringTotal = await _subscriptionRepository.CountExpiringSubscriptionsAsync(now, 7, cancellationToken);
        var expiringList = await _subscriptionRepository.ListExpiringSubscriptionsAsync(now, 7, 5, cancellationToken);
        var expiring = expiringList.Select(s => new ExpiringSubscriptionDto(
            s.Id,
            s.Student?.DisplayName ?? "",
            s.Student?.Email ?? "",
            s.Plan?.Name ?? "",
            s.ExpiresAtUtc!.Value,
            Math.Max(0, (int)(s.ExpiresAtUtc!.Value - now).TotalDays)
        )).ToList();

        var failedTotal = await _paymentRepository.CountFailedPaymentsAsync(sinceUtc, untilUtc, cancellationToken);
        var failedList = await _paymentRepository.ListFailedPaymentsAsync(sinceUtc, untilUtc, 5, cancellationToken);
        var failed = failedList.Select(p => new FailedPaymentDto(
            p.Id,
            p.Student?.DisplayName ?? "",
            p.Student?.Email ?? "",
            p.Plan?.Name ?? "",
            p.Amount,
            p.UpdatedAtUtc
        )).ToList();

        var usersNearLimit = activeSubscriptions
            .Where(s => s.TokenQuotaAtActivation > 0
                && (decimal)s.TotalTokensUsed / s.TokenQuotaAtActivation >= 0.8m)
            .Select(s =>
            {
                var remaining = Math.Max(0, s.TokenQuotaAtActivation - s.TotalTokensUsed);
                var util = Math.Round((decimal)s.TotalTokensUsed / s.TokenQuotaAtActivation * 100, 1);
                return new UserTokenUsageDto(
                    s.StudentUserId,
                    s.Student?.DisplayName ?? "",
                    s.Student?.Email ?? "",
                    s.Plan?.Name ?? "",
                    s.TotalTokensUsed,
                    s.TokenQuotaAtActivation,
                    remaining,
                    util);
            })
            .OrderByDescending(u => u.Utilization)
            .Take(5)
            .ToList();

        var needsAttentionCount = expiringTotal + failedTotal + usersNearLimit.Count;

        // Plan stats (kept for compatibility)
        var counts = await _subscriptionRepository.CountActiveByPlanAsync(now, cancellationToken);
        var statsByPlan = counts.ToDictionary(x => x.PlanId);
        var planStats = plans.Select(plan =>
        {
            var stats = statsByPlan.GetValueOrDefault(plan.Id);
            return new SubscriptionPlanStatsDto(
                plan.Id, plan.Name, plan.Code, plan.IsActive, plan.IsDefault,
                stats?.Count ?? 0, stats?.ActiveValue ?? 0m);
        }).ToList();

        // Recent activity
        var recent = await _subscriptionRepository.ListRecentSubscriptionsAsync(10, cancellationToken);
        var recentPayments = (await _paymentRepository.ListRecentAsync(10, cancellationToken))
            .Select(x => ToRecentPaymentDto(x, pendingSince)).ToList();

        return new SubscriptionDashboardDto(
            await _subscriptionRepository.CountStudentsAsync(cancellationToken),
            activeCount,
            paidActivations,
            payments.RevenueThisMonth,
            totalTokensUsed,
            totalTokenQuota,
            tokenUtilization,
            needsAttentionCount,
            trend,
            packagePerformance,
            expiring,
            expiringTotal,
            failed,
            failedTotal,
            usersNearLimit,
            planStats,
            recent.Select(x => ToRecentDto(x, now)).ToList(),
            recentPayments,
            plans.Select(ToPlanDto).ToList());
    }

    public async Task<IReadOnlyList<ActiveSubscriptionDetailDto>> GetActiveSubscriptionDetailsAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var active = await _subscriptionRepository.ListActiveSubscriptionsAsync(now, cancellationToken);
        return active.Select(s =>
        {
            var remaining = Math.Max(0, s.TokenQuotaAtActivation - s.TotalTokensUsed);
            var utilization = s.TokenQuotaAtActivation > 0
                ? Math.Round((decimal)s.TotalTokensUsed / s.TokenQuotaAtActivation * 100, 1)
                : 0m;
            return new ActiveSubscriptionDetailDto(
                s.Id,
                s.Student?.DisplayName ?? "",
                s.Student?.Email ?? "",
                s.Plan?.Code ?? "",
                s.Plan?.Name ?? "",
                s.TotalTokensUsed,
                s.TokenQuotaAtActivation,
                remaining,
                utilization,
                s.StartedAtUtc,
                s.ExpiresAtUtc);
        }).ToList();
    }

    public async Task CreatePlanAsync(
        string code,
        string name,
        string? description,
        decimal price,
        int durationDays,
        long tokenQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        var isDefault = isActive
            && price == 0
            && await _subscriptionRepository.GetDefaultPlanAsync(cancellationToken) is null;
        ValidatePlan(price, durationDays, tokenQuota, isActive, isDefault);

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
            Price = price,
            DurationDays = durationDays,
            TokenQuota = tokenQuota,
            SortOrder = sortOrder,
            IsActive = isActive,
            IsDefault = isDefault,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);
    }

    public async Task UpdatePlanAsync(
        Guid id,
        string code,
        string name,
        string? description,
        decimal price,
        int durationDays,
        long tokenQuota,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        ValidatePlan(price, durationDays, tokenQuota, isActive, plan.IsDefault);

        if (await _subscriptionRepository.PlanCodeExistsAsync(code, id, cancellationToken))
        {
            throw new InvalidOperationException("Subscription package code already exists.");
        }

        plan.Code = code;
        plan.Name = name;
        plan.Description = description?.Trim() ?? "";
        plan.Price = price;
        plan.DurationDays = durationDays;
        plan.TokenQuota = tokenQuota;
        plan.SortOrder = sortOrder;
        plan.IsActive = isActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SavePlanAsync(plan, cancellationToken);
    }

    public async Task SetDefaultPlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(planId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");
        if (!plan.IsActive || plan.Price != 0)
        {
            throw new InvalidOperationException(
                "Only an active package priced at 0 VND can be the default package.");
        }

        if (plan.IsDefault)
        {
            return;
        }

        plan.IsDefault = true;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SavePlanAsync(plan, cancellationToken);
    }

    private async Task<IReadOnlyList<StudentSubscription>> GetPaidSubscriptionsInRangeAsync(
        DateTime sinceUtc,
        DateTime untilUtc,
        CancellationToken cancellationToken)
    {
        // Get active subscriptions that were paid (PriceAtActivation > 0) and activated in the time range.
        // For demo-scale data, querying all active subs is acceptable.
        var activeSubs = await _subscriptionRepository.ListActiveSubscriptionsAsync(DateTime.UtcNow, cancellationToken);
        return activeSubs
            .Where(s => s.PriceAtActivation > 0
                && s.StartedAtUtc >= sinceUtc && s.StartedAtUtc < untilUtc)
            .ToList();
    }

    private static void ValidatePlan(
        decimal price,
        int durationDays,
        long tokenQuota,
        bool isActive,
        bool isDefault)
    {
        if (price < 0)
        {
            throw new InvalidOperationException("Package price cannot be negative.");
        }

        if (price != decimal.Truncate(price))
        {
            throw new InvalidOperationException("Package price must be a whole VND amount.");
        }

        if (durationDays <= 0)
        {
            throw new InvalidOperationException("Duration days must be greater than zero.");
        }

        if (tokenQuota <= 0)
        {
            throw new InvalidOperationException("Token quota must be greater than zero.");
        }

        // The default package is what new students are attached to, so it must never
        // be turned off or priced: losing it leaves new students without a subscription.
        if (isDefault && !isActive)
        {
            throw new InvalidOperationException(
                "The default package cannot be deactivated. Make another active 0 VND package the default first.");
        }

        if (isDefault && price != 0)
        {
            throw new InvalidOperationException(
                "The default package must keep a price of 0 VND. Make another active 0 VND package the default first.");
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
            plan.Price,
            plan.DurationDays,
            plan.TokenQuota,
            plan.SortOrder,
            plan.IsActive,
            plan.IsDefault);
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
            subscription.PriceAtActivation == 0,
            subscription.PriceAtActivation,
            subscription.TokenQuotaAtActivation,
            subscription.InputTokensUsed,
            subscription.OutputTokensUsed,
            subscription.TotalTokensUsed,
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
