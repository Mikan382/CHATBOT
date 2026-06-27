using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class BenchmarkController : Controller
{
    private readonly EvaluationService _evaluationService;
    private readonly BenchmarkJobRunner _jobRunner;

    public BenchmarkController(EvaluationService evaluationService, BenchmarkJobRunner jobRunner)
    {
        _evaluationService = evaluationService;
        _jobRunner = jobRunner;
    }

    [HttpGet("/benchmark")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var data = await _evaluationService.GetDashboardDataAsync(cancellationToken);
        return View(new BenchmarkIndexViewModel
        {
            Questions = data.Questions,
            Results = data.Results,
            FineTuneConfigured = _evaluationService.FineTuneConfigured,
            GeminiConfigured = _evaluationService.GeminiConfigured,
            ChunkingStrategies = _evaluationService.AvailableChunkingStrategies,
            EmbeddingModels = _evaluationService.AvailableEmbeddingModels
        });
    }

    [HttpPost("/api/evaluations/run")]
    public async Task<IActionResult> Run([FromBody] RunEvaluationRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, error = "Request body is required." });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return BadRequest(new { success = false, errors });
        }

        var results = await _evaluationService.RunAsync(
            request.Limit ?? 5,
            request.ChunkingStrategy,
            request.EmbeddingModel,
            cancellationToken);
        return Json(new { success = true, count = results.Count, results });
    }

    [HttpPost("/api/evaluations/run-full")]
    public IActionResult RunFull([FromBody] RunFullBenchmarkRequest? request)
    {
        var limit = request?.QuestionLimit ?? 5;
        if (!_jobRunner.TryStart(limit, out var error))
        {
            return Json(new { success = false, error });
        }

        return Json(new { success = true, message = "Benchmark started in background. Poll /api/evaluations/progress for status." });
    }

    [HttpGet("/api/evaluations/progress")]
    public IActionResult Progress()
    {
        var progress = _jobRunner.GetProgress();
        return Json(new
        {
            running = progress.Running,
            total = progress.Total,
            done = progress.Done,
            percent = progress.Total > 0 ? (int)(progress.Done * 100.0 / progress.Total) : 0,
            error = progress.Error
        });
    }

    [HttpGet("/api/evaluations/results")]
    public async Task<IActionResult> Results(CancellationToken cancellationToken)
    {
        var results = await _evaluationService.ListResultsAsync(cancellationToken);
        return Json(new { success = true, results });
    }

    [HttpGet("/api/evaluations/export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var results = await _evaluationService.ListResultsAsync(cancellationToken);
        return Json(results);
    }

    public class RunEvaluationRequest
    {
        [Range(1, 50, ErrorMessage = "Limit must be between 1 and 50.")]
        public int? Limit { get; set; }
        public string? ChunkingStrategy { get; set; }
        public string? EmbeddingModel { get; set; }
    }

    public class RunFullBenchmarkRequest
    {
        [Range(1, 50, ErrorMessage = "Question limit must be between 1 and 50.")]
        public int? QuestionLimit { get; set; }
    }
}
