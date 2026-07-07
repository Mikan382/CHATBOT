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
            selectedCourseId = session?.CourseId ?? selectedCourseId;
        }

        var model = new ChatIndexViewModel
        {
            SessionId = sessionId ?? Guid.NewGuid(),
            SelectedCourseId = selectedCourseId,
            Courses = courses,
            FineTuneConfigured = _chatService.FineTuneConfigured,
            GeminiConfigured = _chatService.GeminiConfigured
        };

        return View(model);
    }
}
