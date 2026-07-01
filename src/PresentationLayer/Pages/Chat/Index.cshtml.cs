using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;

namespace PresentationLayer.Pages.Chat;

[Authorize(Roles = UserRoleNames.All)]
public class IndexModel : PageModel
{
    private readonly ChatService _chatService;
    private readonly CourseService _courseService;

    public IndexModel(ChatService chatService, CourseService courseService)
    {
        _chatService = chatService;
        _courseService = courseService;
    }

    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid SelectedCourseId { get; set; }
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public bool FineTuneConfigured { get; set; }
    public bool GeminiConfigured { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? sessionId, Guid? courseId, CancellationToken cancellationToken)
    {
        Courses = await _courseService.ListDtosAsync(cancellationToken);
        SessionId = sessionId ?? Guid.NewGuid();

        if (sessionId.HasValue)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var session = await _chatService.GetSessionAsync(sessionId.Value, userId, cancellationToken);
            SelectedCourseId = session?.CourseId
                ?? courseId
                ?? Courses.FirstOrDefault()?.Id
                ?? Guid.Empty;
        }
        else
        {
            SelectedCourseId = courseId ?? Courses.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        FineTuneConfigured = _chatService.FineTuneConfigured;
        GeminiConfigured = _chatService.GeminiConfigured;
        return Page();
    }
}
