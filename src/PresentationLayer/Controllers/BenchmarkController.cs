using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class BenchmarkController : BaseController
{
    private readonly IEvaluationService _evaluationService;

    public BenchmarkController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _evaluationService.GetDashboardDataAsync(cancellationToken);
        var model = new BenchmarkIndexViewModel
        {
            Dashboard = dashboard,
            FineTuneConfigured = _evaluationService.FineTuneConfigured,
            GeminiConfigured = _evaluationService.GeminiConfigured,
            ChunkingStrategies = _evaluationService.AvailableChunkingStrategies,
            EmbeddingModels = _evaluationService.AvailableEmbeddingModels
        };

        return View(model);
    }
}
