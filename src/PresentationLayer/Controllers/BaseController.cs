using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

/// <summary>
/// Base controller providing shared helpers for all MVC controllers.
/// Eliminates the 4 duplicated CurrentUserId() implementations (fix #12).
/// </summary>
public abstract class BaseController : Controller
{
    protected Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }

    protected void SetFlashSuccess(string message)
    {
        TempData["Success"] = message;
    }

    protected void SetFlashError(string message)
    {
        TempData["Error"] = message;
    }
}
