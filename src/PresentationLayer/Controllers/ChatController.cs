using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.All)]
public class ChatController : Controller
{
    private readonly ChatService _chatService;
    private readonly CourseService _courseService;

    public ChatController(ChatService chatService, CourseService courseService)
    {
        _chatService = chatService;
        _courseService = courseService;
    }

    [HttpGet("/chat")]
    public async Task<IActionResult> Index(Guid? sessionId, Guid? courseId, CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListDtosAsync(cancellationToken);
        var selectedCourseId = courseId ?? courses.FirstOrDefault(x => x.Code == "PRN222")?.Id ?? courses.FirstOrDefault()?.Id ?? Guid.Empty;
        return View(new ChatIndexViewModel
        {
            SessionId = sessionId ?? Guid.NewGuid(),
            Courses = courses,
            SelectedCourseId = selectedCourseId,
            FineTuneConfigured = _chatService.FineTuneConfigured,
            GeminiConfigured = _chatService.GeminiConfigured
        });
    }

    [HttpGet("/api/chat/{sessionId:guid}")]
    public async Task<IActionResult> History(Guid sessionId, CancellationToken cancellationToken)
    {
        return Json(new { success = true, history = await _chatService.GetHistoryAsync(sessionId, CurrentUserId(), cancellationToken) });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }
}
