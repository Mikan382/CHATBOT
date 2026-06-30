using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.Chapters;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class CreateModel : PageModel
{
    private readonly ChapterService _chapterService;
    private readonly CourseService _courseService;

    public CreateModel(ChapterService chapterService, CourseService courseService)
    {
        _chapterService = chapterService;
        _courseService = courseService;
    }

    [BindProperty]
    public ChapterFormViewModel Input { get; set; } = new();

    public IReadOnlyList<CourseDto> Courses { get; set; } = [];

    public async Task OnGetAsync(Guid courseId, CancellationToken cancellationToken)
    {
        Courses = await _courseService.ListDtosAsync(cancellationToken);
        Input.CourseId = courseId;
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Courses = await _courseService.ListDtosAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var chapter = await _chapterService.CreateAsync(Input.CourseId, Input.Order, Input.Clo, Input.Title, Input.Summary, cancellationToken);
            TempData["Success"] = "Chapter was created.";
            return RedirectToPage("/Courses/Chapters", new { id = chapter.CourseId });
        }
        catch (Exception ex)
        {
            Input.Error = ex.Message;
            return Page();
        }
    }
}
