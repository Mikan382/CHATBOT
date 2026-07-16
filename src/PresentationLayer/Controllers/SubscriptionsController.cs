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
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        return View(await BuildDashboardModelAsync(cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(SubscriptionPlanInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Dashboard", await BuildDashboardModelAsync(
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
                cancellationToken,
                createPlan: input,
                error: UserFacingError(ex)));
        }

        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlan(SubscriptionPlanInput input, CancellationToken cancellationToken)
    {
        if (!input.Id.HasValue || !ModelState.IsValid)
        {
            return View("Dashboard", await BuildDashboardModelAsync(
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
                cancellationToken,
                failedPlanUpdate: input,
                error: UserFacingError(ex)));
        }

        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultPlan(Guid planId, CancellationToken cancellationToken)
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

        return RedirectToAction("Dashboard");
    }

    private async Task<SubscriptionDashboardViewModel> BuildDashboardModelAsync(
        CancellationToken cancellationToken,
        SubscriptionPlanInput? createPlan = null,
        SubscriptionPlanInput? failedPlanUpdate = null,
        string? error = null)
    {
        return new SubscriptionDashboardViewModel
        {
            Dashboard = await _subscriptionService.GetDashboardAsync(cancellationToken),
            CreatePlan = createPlan ?? new SubscriptionPlanInput(),
            FailedPlanUpdate = failedPlanUpdate,
            Error = error
        };
    }
}
