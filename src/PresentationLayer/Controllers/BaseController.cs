using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

public abstract class BaseController : Controller
{
    protected Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }

    protected string CurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    protected void SetFlashSuccess(string message)
    {
        TempData["Success"] = message;
    }

    protected void SetFlashError(string message)
    {
        TempData["Error"] = message;
    }

    protected string UserFacingError(Exception exception)
    {
        if (exception is InvalidOperationException)
        {
            return exception.Message;
        }

        var logger = HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType());
        logger.LogError(exception, "Request failed in {Controller}.", GetType().Name);
        return "The request could not be completed. Please try again.";
    }
}
