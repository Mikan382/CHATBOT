using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;

namespace PresentationLayer.ApiControllers;

[ApiController]
[Authorize(Roles = "Teacher,Admin")]
public class BenchmarkApiController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly IBenchmarkJobRunner _jobRunner;

    public BenchmarkApiController(IEvaluationService evaluationService, IBenchmarkJobRunner jobRunner)
    {
        _evaluationService = evaluationService;
        _jobRunner = jobRunner;
    }

    [HttpPost("/api/evaluations/run")]
    [ValidateAntiForgeryToken]
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

        var count = await _evaluationService.RunAsync(
            request.Limit ?? 5,
            request.ChunkingStrategy,
            request.EmbeddingModel,
            cancellationToken);
        return Ok(new { success = true, count });
    }

    [HttpPost("/api/evaluations/run-full")]
    [ValidateAntiForgeryToken]
    public IActionResult RunFull([FromBody] RunFullBenchmarkRequest? request)
    {
        var limit = request?.QuestionLimit ?? 5;
        if (!_jobRunner.TryStart(limit, out var error))
        {
            return Ok(new { success = false, error });
        }

        return Ok(new { success = true, message = "Benchmark started in background. Poll /api/evaluations/progress for status." });
    }

    [HttpGet("/api/evaluations/progress")]
    public IActionResult Progress()
    {
        var progress = _jobRunner.GetProgress();
        return Ok(new
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
        return Ok(new { success = true, results });
    }

    [HttpGet("/api/evaluations/export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var results = await _evaluationService.ListResultsAsync(cancellationToken);
        return Ok(results);
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
