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
            PendingRequest = data.PendingRequest,
            AvailablePlans = data.AvailablePlans
        });
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPackage(Guid planId, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.RequestSubscriptionAsync(CurrentUserId(), planId, cancellationToken);
            SetFlashSuccess("Subscription request was submitted for admin approval.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.CancelPendingRequestAsync(CurrentUserId(), cancellationToken);
            SetFlashSuccess("Pending subscription request was cancelled.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSubscription(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.RevokeSubscriptionAsync(id, cancellationToken);
            SetFlashSuccess("Subscription was revoked.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        return View(new SubscriptionDashboardViewModel
        {
            Dashboard = await _subscriptionService.GetDashboardAsync(cancellationToken)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.ApproveRequestAsync(id, cancellationToken);
            SetFlashSuccess("Subscription request was approved.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.RejectRequestAsync(id, cancellationToken);
            SetFlashSuccess("Subscription request was rejected.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(SubscriptionPlanInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetFlashError("Please check the subscription package fields.");
            return RedirectToAction("Dashboard");
        }

        try
        {
            await _subscriptionService.CreatePlanAsync(
                input.Code,
                input.Name,
                input.Description,
                input.MonthlyPrice,
                input.DurationDays,
                input.MessageQuota,
                input.SortOrder,
                input.IsActive,
                cancellationToken);
            SetFlashSuccess("Subscription package was created.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
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
            SetFlashError("Please check the subscription package fields.");
            return RedirectToAction("Dashboard");
        }

        try
        {
            await _subscriptionService.UpdatePlanAsync(
                input.Id.Value,
                input.Code,
                input.Name,
                input.Description,
                input.MonthlyPrice,
                input.DurationDays,
                input.MessageQuota,
                input.SortOrder,
                input.IsActive,
                cancellationToken);
            SetFlashSuccess("Subscription package was updated.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Dashboard");
    }

}
