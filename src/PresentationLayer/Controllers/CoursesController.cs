using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class CoursesController : BaseController
{
    private readonly ICourseService _courseService;
    private readonly IChapterService _chapterService;

    public CoursesController(ICourseService courseService, IChapterService chapterService)
    {
        _courseService = courseService;
        _chapterService = chapterService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListAsync(searchTerm, cancellationToken);
        ViewBag.SearchTerm = searchTerm;
        return View(courses);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CourseFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _courseService.CreateAsync(model.Code, model.Name, model.Description, model.Tools, cancellationToken);
            SetFlashSuccess("Course was created.");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            model.Error = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetEditableAsync(id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        var model = new CourseFormViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            Description = course.Description,
            Tools = course.Tools
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CourseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(model);
        }

        try
        {
            await _courseService.UpdateAsync(id, model.Code, model.Name, model.Description, model.Tools, cancellationToken);
            SetFlashSuccess("Course was updated.");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            model.Id = id;
            model.Error = ex.Message;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseService.DeleteAsync(id, cancellationToken);
            SetFlashSuccess("Course was deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Chapters(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetDetailsAsync(id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        var chapters = await _courseService.ListChaptersAsync(id, cancellationToken);
        ViewBag.Course = course;
        return View(chapters);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChapter(Guid id, Guid chapterId, CancellationToken cancellationToken)
    {
        try
        {
            await _chapterService.DeleteAsync(chapterId, cancellationToken);
            SetFlashSuccess("Chapter was deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Chapters", new { id });
    }
}
