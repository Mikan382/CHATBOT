using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class ChaptersController : BaseController
{
    private readonly IChapterService _chapterService;
    private readonly ICourseService _courseService;

    public ChaptersController(IChapterService chapterService, ICourseService courseService)
    {
        _chapterService = chapterService;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid courseId, CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListManageDtosAsync(CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        var model = new ChapterFormViewModel { CourseId = courseId };
        ViewBag.Courses = courses;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChapterFormViewModel model, CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListManageDtosAsync(CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        ViewBag.Courses = courses;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _chapterService.CreateAsync(model.CourseId, model.Order, model.Clo, model.Title, model.Summary, CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
            SetFlashSuccess("Chapter was created.");
            return RedirectToAction("Chapters", "Courses", new { id = result.CourseId });
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
        var chapter = await _chapterService.GetEditableAsync(id, CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        if (chapter is null)
        {
            return NotFound();
        }

        var courses = await _courseService.ListManageDtosAsync(CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        ViewBag.Courses = courses;
        var model = new ChapterFormViewModel
        {
            Id = chapter.Id,
            CourseId = chapter.CourseId,
            Order = chapter.Order,
            Clo = chapter.Clo,
            Title = chapter.Title,
            Summary = chapter.Summary
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ChapterFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        var courses = await _courseService.ListManageDtosAsync(CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        ViewBag.Courses = courses;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _chapterService.UpdateAsync(id, model.CourseId, model.Order, model.Clo, model.Title, model.Summary, CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
            SetFlashSuccess("Chapter was updated.");
            return RedirectToAction("Chapters", "Courses", new { id = model.CourseId });
        }
        catch (Exception ex)
        {
            model.Error = ex.Message;
            return View(model);
        }
    }
}
