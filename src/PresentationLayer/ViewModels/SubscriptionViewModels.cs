using System.ComponentModel.DataAnnotations;
using BusinessLayer.DTOs;

namespace PresentationLayer.ViewModels;

public class StudentSubscriptionViewModel
{
    public StudentSubscriptionDto? CurrentSubscription { get; set; }
    public IReadOnlyList<SubscriptionPlanDto> AvailablePlans { get; set; } = [];
    public bool PaymentConfigured { get; set; }
}

public class SubscriptionDashboardViewModel
{
    public SubscriptionDashboardDto Dashboard { get; set; } = null!;
    public SubscriptionPlanInput CreatePlan { get; set; } = new();
    public SubscriptionPlanInput? FailedPlanUpdate { get; set; }
    public string? Error { get; set; }
    public int PeriodDays { get; set; } = 30;
}

public class SubscriptionPlanInput
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(160)]
    public string Name { get; set; } = "";

    [StringLength(600)]
    public string? Description { get; set; }

    [Range(0, 100000000)]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    public int DurationDays { get; set; } = 30;

    [Range(1, 10000000000)]
    public long TokenQuota { get; set; } = 50000;

    [Range(0, 10000)]
    public int SortOrder { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}
