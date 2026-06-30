using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;

namespace PresentationLayer.Pages.Courses;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class IndexModel : PageModel
{
    private readonly CourseService _courseService;

    public IndexModel(CourseService courseService)
    {
        _courseService = courseService;
    }

    public IReadOnlyList<CourseListDto> CourseList { get; set; } = [];
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        SearchTerm = searchTerm;
        CourseList = await _courseService.ListAsync(searchTerm, cancellationToken);
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Course was deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}
