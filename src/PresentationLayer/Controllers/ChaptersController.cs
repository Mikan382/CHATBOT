using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class ChaptersController : Controller
{
    private readonly ChapterService _chapterService;
    private readonly CourseService _courseService;

    public ChaptersController(ChapterService chapterService, CourseService courseService)
    {
        _chapterService = chapterService;
        _courseService = courseService;
    }

    [HttpGet("/chapters/create")]
    public async Task<IActionResult> Create(Guid courseId, CancellationToken cancellationToken)
    {
        return View("Form", new ChapterFormViewModel
        {
            CourseId = courseId,
            Courses = await _courseService.ListDtosAsync(cancellationToken)
        });
    }

    [HttpPost("/chapters/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChapterFormViewModel input, CancellationToken cancellationToken)
    {
        input.Courses = await _courseService.ListDtosAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Form", input);
        }

        try
        {
            var chapter = await _chapterService.CreateAsync(input.CourseId, input.Order, input.Clo, input.Title, input.Summary, cancellationToken);
            TempData["Success"] = "Chapter was created.";
            return RedirectToAction("Chapters", "Courses", new { id = chapter.CourseId });
        }
        catch (Exception ex)
        {
            input.Error = ex.Message;
            return View("Form", input);
        }
    }

    [HttpGet("/chapters/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var chapter = await _chapterService.GetEditableAsync(id, cancellationToken);
        if (chapter is null)
        {
            return NotFound();
        }

        return View("Form", new ChapterFormViewModel
        {
            Id = chapter.Id,
            CourseId = chapter.CourseId,
            Order = chapter.Order,
            Clo = chapter.Clo,
            Title = chapter.Title,
            Summary = chapter.Summary,
            Courses = await _courseService.ListDtosAsync(cancellationToken)
        });
    }

    [HttpPost("/chapters/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ChapterFormViewModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        input.Courses = await _courseService.ListDtosAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Form", input);
        }

        try
        {
            await _chapterService.UpdateAsync(id, input.CourseId, input.Order, input.Clo, input.Title, input.Summary, cancellationToken);
            TempData["Success"] = "Chapter was updated.";
            return RedirectToAction("Chapters", "Courses", new { id = input.CourseId });
        }
        catch (Exception ex)
        {
            input.Error = ex.Message;
            return View("Form", input);
        }
    }

    [HttpPost("/chapters/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid courseId, CancellationToken cancellationToken)
    {
        try
        {
            await _chapterService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Chapter was deleted.";
            return RedirectToAction("Chapters", "Courses", new { id = courseId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Chapters", "Courses", new { id = courseId });
        }
    }
}
