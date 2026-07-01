using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;

namespace PresentationLayer.Pages.Courses;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class ChaptersModel : PageModel
{
    private readonly CourseService _courseService;
    private readonly ChapterService _chapterService;

    public ChaptersModel(CourseService courseService, ChapterService chapterService)
    {
        _courseService = courseService;
        _chapterService = chapterService;
    }

    public CourseDto Course { get; set; } = new(Guid.Empty, "", "", "", "", []);
    public IReadOnlyList<ChapterDto> ChapterList { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetDetailsAsync(id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        Course = course;
        ChapterList = await _courseService.ListChaptersAsync(id, cancellationToken);
        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid chapterId, CancellationToken cancellationToken)
    {
        try
        {
            await _chapterService.DeleteAsync(chapterId, cancellationToken);
            TempData["Success"] = "Chapter was deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}
