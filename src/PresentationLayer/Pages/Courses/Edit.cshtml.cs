using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.Courses;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class EditModel : PageModel
{
    private readonly CourseService _courseService;

    public EditModel(CourseService courseService)
    {
        _courseService = courseService;
    }

    [BindProperty]
    public CourseFormViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetEditableAsync(id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        Input = new CourseFormViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            Description = course.Description,
            Tools = course.Tools
        };
        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Input.Id = id;
            return Page();
        }

        try
        {
            await _courseService.UpdateAsync(id, Input.Code, Input.Name, Input.Description, Input.Tools, cancellationToken);
            TempData["Success"] = "Course was updated.";
            return RedirectToPage("/Courses/Index");
        }
        catch (Exception ex)
        {
            Input.Id = id;
            Input.Error = ex.Message;
            return Page();
        }
    }
}
