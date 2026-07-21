using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class CoursesController : BaseController
{
    private readonly ICourseService _courseService;
    private readonly IChapterService _chapterService;
    private readonly IDocumentService _documentService;

    public CoursesController(
        ICourseService courseService,
        IChapterService chapterService,
        IDocumentService documentService)
    {
        _courseService = courseService;
        _chapterService = chapterService;
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListManageAsync(searchTerm, CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        return View(new CourseIndexViewModel
        {
            Courses = courses,
            SearchTerm = searchTerm,
            CanManageCourses = User.IsInRole("Admin")
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new CourseFormViewModel
        {
            Teachers = await _courseService.ListTeacherOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Teachers = await _courseService.ListTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _courseService.CreateAsync(
                model.Code,
                model.Name,
                model.Description,
                model.Tools,
                model.TeacherId,
                model.DefaultChunkingStrategy,
                model.DefaultChunkSize,
                model.DefaultChunkOverlap,
                model.DefaultEmbeddingModel,
                cancellationToken);
            SetFlashSuccess("Course was created.");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            model.Error = UserFacingError(ex);
            model.Teachers = await _courseService.ListTeacherOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
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
            Tools = course.Tools,
            TeacherId = course.TeacherId,
            DefaultChunkingStrategy = course.DefaultChunkingStrategy,
            DefaultChunkSize = course.DefaultChunkSize,
            DefaultChunkOverlap = course.DefaultChunkOverlap,
            DefaultEmbeddingModel = course.DefaultEmbeddingModel,
            Teachers = await _courseService.ListTeacherOptionsAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CourseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Teachers = await _courseService.ListTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _courseService.UpdateAsync(
                id,
                model.Code,
                model.Name,
                model.Description,
                model.Tools,
                model.TeacherId,
                model.DefaultChunkingStrategy,
                model.DefaultChunkSize,
                model.DefaultChunkOverlap,
                model.DefaultEmbeddingModel,
                cancellationToken);
            SetFlashSuccess("Course was updated.");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            model.Id = id;
            model.Error = UserFacingError(ex);
            model.Teachers = await _courseService.ListTeacherOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Chapters(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetDetailsAsync(id, CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        return View(new CourseChaptersViewModel { Course = course });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChapter(Guid id, Guid chapterId, CancellationToken cancellationToken)
    {
        try
        {
            await _chapterService.DeleteAsync(chapterId, CurrentUserId(), CurrentUserRole(), cancellationToken);
            SetFlashSuccess("Chapter was deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Chapters", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reindex(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _documentService.ReindexCourseAsync(id, CurrentUserId(), CurrentUserRole(), cancellationToken);
            SetFlashSuccess($"Successfully re-indexed {count} document(s) for this course with current AI settings.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Index");
    }
}
