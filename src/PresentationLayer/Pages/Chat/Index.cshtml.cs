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
        SelectedCourseId = courseId ?? Courses.FirstOrDefault(x => x.Code == "PRN222")?.Id ?? Courses.FirstOrDefault()?.Id ?? Guid.Empty;
        SessionId = sessionId ?? Guid.NewGuid();
        FineTuneConfigured = _chatService.FineTuneConfigured;
        GeminiConfigured = _chatService.GeminiConfigured;
        return Page();
    }
}
