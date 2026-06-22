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

    public BenchmarkController(EvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
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
    public async Task<IActionResult> RunFull([FromBody] RunFullBenchmarkRequest? request, CancellationToken cancellationToken)
    {
        var limit = request?.QuestionLimit ?? 5;
        var results = await _evaluationService.RunFullBenchmarkAsync(limit, cancellationToken);
        return Json(new { success = true, count = results.Count, results });
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
