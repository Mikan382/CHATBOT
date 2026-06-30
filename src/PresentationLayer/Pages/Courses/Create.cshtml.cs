using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.Courses;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class CreateModel : PageModel
{
    private readonly CourseService _courseService;

    public CreateModel(CourseService courseService)
    {
        _courseService = courseService;
    }

    [BindProperty]
    public CourseFormViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _courseService.CreateAsync(Input.Code, Input.Name, Input.Description, Input.Tools, cancellationToken);
            TempData["Success"] = "Course was created.";
            return RedirectToPage("/Courses/Index");
        }
        catch (Exception ex)
        {
            Input.Error = ex.Message;
            return Page();
        }
    }
}
