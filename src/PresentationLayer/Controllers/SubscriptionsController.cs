using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize]
public class SubscriptionsController : BaseController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Dashboard");
        }

        if (!User.IsInRole("Student"))
        {
            SetFlashError("Only students can register for service packages.");
            return RedirectToAction("Index", "Chat");
        }

        var data = await _subscriptionService.GetStudentPageAsync(CurrentUserId(), cancellationToken);
        return View(new StudentSubscriptionViewModel
        {
            CurrentSubscription = data.CurrentSubscription,
            AvailablePlans = data.AvailablePlans,
            PaymentConfigured = data.PaymentConfigured
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Dashboard(int? period, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var periodDays = period switch
        {
            7 => 7,
            0 => 0,
            _ => 30
        };
        return View(await BuildDashboardModelAsync(periodDays, startDate, endDate, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> ActiveSubscriptionDetails(CancellationToken cancellationToken)
    {
        var details = await _subscriptionService.GetActiveSubscriptionDetailsAsync(cancellationToken);
        return Json(details);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(SubscriptionPlanInput input, int? period, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var periodDays = period switch { 7 => 7, 0 => 0, _ => 30 };
        if (!ModelState.IsValid)
        {
            return View("Dashboard", await BuildDashboardModelAsync(
                periodDays,
                startDate,
                endDate,
                cancellationToken,
                createPlan: input,
                error: "Please check the subscription package fields."));
        }

        try
        {
            await _subscriptionService.CreatePlanAsync(
                input.Code,
                input.Name,
                input.Description,
                input.Price,
                input.DurationDays,
                input.TokenQuota,
                input.SortOrder,
                input.IsActive,
                cancellationToken);
            SetFlashSuccess("Subscription package was created.");
        }
        catch (Exception ex)
        {
            return View("Dashboard", await BuildDashboardModelAsync(
                periodDays,
                startDate,
                endDate,
                cancellationToken,
                createPlan: input,
                error: UserFacingError(ex)));
        }

        return RedirectToAction("Dashboard", new { period, startDate = startDate?.ToString("yyyy-MM-dd"), endDate = endDate?.ToString("yyyy-MM-dd") });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlan(SubscriptionPlanInput input, int? period, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var periodDays = period switch { 7 => 7, 0 => 0, _ => 30 };
        if (!input.Id.HasValue || !ModelState.IsValid)
        {
            return View("Dashboard", await BuildDashboardModelAsync(
                periodDays,
                startDate,
                endDate,
                cancellationToken,
                failedPlanUpdate: input,
                error: "Please check the subscription package fields."));
        }

        try
        {
            await _subscriptionService.UpdatePlanAsync(
                input.Id.Value,
                input.Code,
                input.Name,
                input.Description,
                input.Price,
                input.DurationDays,
                input.TokenQuota,
                input.SortOrder,
                input.IsActive,
                cancellationToken);
            SetFlashSuccess("Subscription package was updated.");
        }
        catch (Exception ex)
        {
            return View("Dashboard", await BuildDashboardModelAsync(
                periodDays,
                startDate,
                endDate,
                cancellationToken,
                failedPlanUpdate: input,
                error: UserFacingError(ex)));
        }

        return RedirectToAction("Dashboard", new { period, startDate = startDate?.ToString("yyyy-MM-dd"), endDate = endDate?.ToString("yyyy-MM-dd") });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultPlan(Guid planId, int? period, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.SetDefaultPlanAsync(planId, cancellationToken);
            SetFlashSuccess("Default subscription package was updated.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Dashboard", new { period, startDate = startDate?.ToString("yyyy-MM-dd"), endDate = endDate?.ToString("yyyy-MM-dd") });
    }

    private async Task<SubscriptionDashboardViewModel> BuildDashboardModelAsync(
        int periodDays,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken,
        SubscriptionPlanInput? createPlan = null,
        SubscriptionPlanInput? failedPlanUpdate = null,
        string? error = null)
    {
        return new SubscriptionDashboardViewModel
        {
            Dashboard = await _subscriptionService.GetDashboardAsync(periodDays, startDate, endDate, cancellationToken),
            CreatePlan = createPlan ?? new SubscriptionPlanInput(),
            FailedPlanUpdate = failedPlanUpdate,
            Error = error,
            PeriodDays = periodDays,
            StartDate = startDate,
            EndDate = endDate
        };
    }
}
