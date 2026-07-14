using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Admin")]
public class BenchmarkController : BaseController
{
    private readonly IBenchmarkService _benchmarkService;

    public BenchmarkController(IBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseId, CancellationToken cancellationToken)
    {
        return View(new BenchmarkDashboardViewModel
        {
            Dashboard = await _benchmarkService.GetDashboardAsync(courseId, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(BenchmarkRunInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !input.CourseId.HasValue)
        {
            SetFlashError("Select a course, chunking strategy, embedding model, and valid Top K value.");
            return RedirectToAction("Index", new { courseId = input.CourseId });
        }

        try
        {
            var runId = await _benchmarkService.RunAsync(
                input.CourseId.Value,
                input.ChunkingStrategy,
                input.EmbeddingModel,
                input.TopK,
                cancellationToken);
            SetFlashSuccess("Benchmark run was completed.");
            return RedirectToAction("Details", new { id = runId });
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
            return RedirectToAction("Index", new { courseId = input.CourseId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Questions(
        Guid? courseId,
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            return View(new BenchmarkQuestionPageViewModel
            {
                Page = await _benchmarkService.ListQuestionsAsync(
                    courseId,
                    searchTerm,
                    isActive,
                    cancellationToken)
            });
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
            return RedirectToAction("Questions", new { courseId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreateQuestion(Guid? courseId, CancellationToken cancellationToken)
    {
        var editor = await _benchmarkService.GetCreateQuestionAsync(courseId, cancellationToken);
        return View("QuestionForm", new BenchmarkQuestionFormViewModel
        {
            CourseId = editor.SelectedCourseId,
            DisplayOrder = editor.SuggestedDisplayOrder,
            Courses = editor.Courses,
            Documents = editor.Documents
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(
        BenchmarkQuestionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !model.CourseId.HasValue || !model.ExpectedDocumentId.HasValue)
        {
            await PopulateQuestionOptionsAsync(model, cancellationToken);
            return View("QuestionForm", model);
        }

        try
        {
            await _benchmarkService.CreateQuestionAsync(
                model.CourseId.Value,
                model.ExpectedDocumentId.Value,
                model.Question,
                model.ExpectedAnswer,
                model.DisplayOrder,
                model.IsActive,
                cancellationToken);
            SetFlashSuccess("Ground-truth question was created.");
            return RedirectToAction("Questions", new { courseId = model.CourseId });
        }
        catch (Exception ex)
        {
            model.Error = UserFacingError(ex);
            await PopulateQuestionOptionsAsync(model, cancellationToken);
            return View("QuestionForm", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditQuestion(Guid id, CancellationToken cancellationToken)
    {
        var editor = await _benchmarkService.GetEditQuestionAsync(id, cancellationToken);
        if (editor?.Question is null)
        {
            return NotFound();
        }

        return View("QuestionForm", new BenchmarkQuestionFormViewModel
        {
            Id = editor.Question.Id,
            CourseId = editor.Question.CourseId,
            ExpectedDocumentId = editor.Question.ExpectedDocumentId,
            Question = editor.Question.Question,
            ExpectedAnswer = editor.Question.ExpectedAnswer,
            DisplayOrder = editor.Question.DisplayOrder,
            IsActive = editor.Question.IsActive,
            Courses = editor.Courses,
            Documents = editor.Documents
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(
        Guid id,
        BenchmarkQuestionFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid || !model.CourseId.HasValue || !model.ExpectedDocumentId.HasValue)
        {
            await PopulateQuestionOptionsAsync(model, cancellationToken);
            return View("QuestionForm", model);
        }

        try
        {
            await _benchmarkService.UpdateQuestionAsync(
                id,
                model.CourseId.Value,
                model.ExpectedDocumentId.Value,
                model.Question,
                model.ExpectedAnswer,
                model.DisplayOrder,
                model.IsActive,
                cancellationToken);
            SetFlashSuccess("Ground-truth question was updated.");
            return RedirectToAction("Questions", new { courseId = model.CourseId });
        }
        catch (Exception ex)
        {
            model.Error = UserFacingError(ex);
            await PopulateQuestionOptionsAsync(model, cancellationToken);
            return View("QuestionForm", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(Guid id, Guid? courseId, CancellationToken cancellationToken)
    {
        try
        {
            await _benchmarkService.DeleteQuestionAsync(id, cancellationToken);
            SetFlashSuccess("Ground-truth question was deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Questions", new { courseId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var details = await _benchmarkService.GetRunAsync(id, cancellationToken);
        return details is null
            ? NotFound()
            : View(new BenchmarkRunDetailsViewModel { Details = details });
    }

    private async Task PopulateQuestionOptionsAsync(
        BenchmarkQuestionFormViewModel model,
        CancellationToken cancellationToken)
    {
        var editor = model.Id.HasValue
            ? await _benchmarkService.GetEditQuestionAsync(model.Id.Value, cancellationToken)
            : await _benchmarkService.GetCreateQuestionAsync(model.CourseId, cancellationToken);
        if (editor is null)
        {
            return;
        }

        model.Courses = editor.Courses;
        model.Documents = editor.Documents;
    }
}
