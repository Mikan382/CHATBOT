using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

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
            GeminiConfigured = _evaluationService.GeminiConfigured
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

        var results = await _evaluationService.RunAsync(request.Limit ?? 5, cancellationToken);
        return Json(new { success = true, count = results.Count, results });
    }

    [HttpGet("/api/evaluations/results")]
    public async Task<IActionResult> Results(CancellationToken cancellationToken)
    {
        var results = await _evaluationService.ListResultsAsync(cancellationToken);
        return Json(new { success = true, results });
    }

    public class RunEvaluationRequest
    {
        [Range(1, 5, ErrorMessage = "Limit must be between 1 and 5.")]
        public int? Limit { get; set; }
    }
}
