using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.Chapters;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class EditModel : PageModel
{
    private readonly ChapterService _chapterService;
    private readonly CourseService _courseService;

    public EditModel(ChapterService chapterService, CourseService courseService)
    {
        _chapterService = chapterService;
        _courseService = courseService;
    }

    [BindProperty]
    public ChapterFormViewModel Input { get; set; } = new();

    public IReadOnlyList<CourseDto> Courses { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var chapter = await _chapterService.GetEditableAsync(id, cancellationToken);
        if (chapter is null)
        {
            return NotFound();
        }

        Courses = await _courseService.ListDtosAsync(cancellationToken);
        Input = new ChapterFormViewModel
        {
            Id = chapter.Id,
            CourseId = chapter.CourseId,
            Order = chapter.Order,
            Clo = chapter.Clo,
            Title = chapter.Title,
            Summary = chapter.Summary
        };
        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        Input.Id = id;
        Courses = await _courseService.ListDtosAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _chapterService.UpdateAsync(id, Input.CourseId, Input.Order, Input.Clo, Input.Title, Input.Summary, cancellationToken);
            TempData["Success"] = "Chapter was updated.";
            return RedirectToPage("/Courses/Chapters", new { id = Input.CourseId });
        }
        catch (Exception ex)
        {
            Input.Error = ex.Message;
            return Page();
        }
    }
}
