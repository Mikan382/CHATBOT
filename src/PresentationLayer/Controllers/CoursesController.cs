using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class CoursesController : Controller
{
    private readonly CourseService _courseService;

    public CoursesController(CourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet("/courses")]
    public async Task<IActionResult> Index(string? searchTerm, CancellationToken cancellationToken)
    {
        return View(new CourseIndexViewModel
        {
            Courses = await _courseService.ListAsync(searchTerm, cancellationToken),
            SearchTerm = searchTerm
        });
    }

    [HttpGet("/courses/create")]
    public IActionResult Create()
    {
        return View("Form", new CourseFormViewModel());
    }

    [HttpPost("/courses/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", input);
        }

        try
        {
            await _courseService.CreateAsync(input.Code, input.Name, input.Description, input.Tools, cancellationToken);
            TempData["Success"] = "Course was created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            input.Error = ex.Message;
            return View("Form", input);
        }
    }

    [HttpGet("/courses/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetEditableAsync(id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        return View("Form", new CourseFormViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            Description = course.Description,
            Tools = course.Tools
        });
    }

    [HttpPost("/courses/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CourseFormViewModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            input.Id = id;
            return View("Form", input);
        }

        try
        {
            await _courseService.UpdateAsync(id, input.Code, input.Name, input.Description, input.Tools, cancellationToken);
            TempData["Success"] = "Course was updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            input.Id = id;
            input.Error = ex.Message;
            return View("Form", input);
        }
    }

    [HttpPost("/courses/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Course was deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("/courses/{id:guid}/chapters")]
    public async Task<IActionResult> Chapters(Guid id, CancellationToken cancellationToken)
    {
        return View(new CourseChaptersViewModel
        {
            Course = await _courseService.GetDetailsAsync(id, cancellationToken),
            Chapters = await _courseService.ListChaptersAsync(id, cancellationToken)
        });
    }
}
