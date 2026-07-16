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

    public async Task<SubscriptionDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var pendingSince = now.Subtract(_paymentGateway.CheckoutLifetime);
        var plans = await _subscriptionRepository.ListPlansAsync(includeInactive: true, cancellationToken);
        var counts = await _subscriptionRepository.CountActiveByPlanAsync(now, cancellationToken);
        var payments = await _paymentRepository.GetDashboardSummaryAsync(monthStart, pendingSince, cancellationToken);
        var tokenUsage = await _subscriptionRepository.GetActiveTokenUsageAsync(now, cancellationToken);
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
                plan.IsDefault,
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
            tokenUsage.InputTokens,
            tokenUsage.OutputTokens,
            tokenUsage.TotalTokens,
            tokenUsage.TokenQuota,
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
        decimal price,
        int durationDays,
        long tokenQuota,
        int sortOrder,
        bool isActive,
        bool isDefault,
        CancellationToken cancellationToken)
    {
        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
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
        bool isDefault,
        CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetPlanAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        code = NormalizeCode(code);
        name = StringHelper.NormalizeRequired(name, "Package name");
        if (plan.IsDefault && !isDefault)
        {
            throw new InvalidOperationException(
                "Select another active free package as default instead of clearing the current default.");
        }

        ValidatePlan(price, durationDays, tokenQuota, isActive, isDefault);

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
        plan.IsDefault = isDefault;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _subscriptionRepository.SavePlanAsync(plan, cancellationToken);
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

        if (isDefault && (!isActive || price != 0))
        {
            throw new InvalidOperationException(
                "The default package must be active and have a price of 0 VND.");
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
