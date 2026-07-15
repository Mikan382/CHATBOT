using System.ComponentModel.DataAnnotations;
using BusinessLayer.DTOs;

namespace PresentationLayer.ViewModels;

public class StudentSubscriptionViewModel
{
    public StudentSubscriptionDto? CurrentSubscription { get; set; }
    public IReadOnlyList<SubscriptionPlanDto> AvailablePlans { get; set; } = [];
}

public class SubscriptionDashboardViewModel
{
    public SubscriptionDashboardDto Dashboard { get; set; } = new(
        0,
        0,
        0,
        0,
        [],
        [],
        []);
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
    public decimal MonthlyPrice { get; set; }

    [Range(0, 3650)]
    public int DurationDays { get; set; } = 30;

    [Range(0, 1000000)]
    public int MessageQuota { get; set; }

    [Range(0, 10000)]
    public int SortOrder { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}
