using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;
    private readonly ICourseService _courseService;

    public ChatController(IChatService chatService, ICourseService courseService)
    {
        _chatService = chatService;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? sessionId, Guid? courseId, CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListDtosAsync(cancellationToken);
        var selectedCourseId = courseId ?? courses.FirstOrDefault()?.Id ?? Guid.Empty;

        if (sessionId.HasValue)
        {
            var session = await _chatService.GetSessionAsync(sessionId.Value, CurrentUserId(), cancellationToken);
            if (session is null)
            {
                return NotFound();
            }

            selectedCourseId = session.CourseId ?? selectedCourseId;
        }

        var model = new ChatIndexViewModel
        {
            SessionId = sessionId ?? Guid.NewGuid(),
            SelectedCourseId = selectedCourseId,
            Courses = courses,
            GeminiConfigured = _chatService.GeminiConfigured
        };

        if (User.IsInRole("Student"))
        {
            model.Quota = await _chatService.GetQuotaStatusAsync(CurrentUserId(), cancellationToken);
        }

        return View(model);
    }
}
