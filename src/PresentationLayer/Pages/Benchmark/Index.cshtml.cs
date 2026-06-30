using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace PresentationLayer.Pages.Benchmark;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class IndexModel : PageModel
{
    private readonly EvaluationService _evaluationService;

    public IndexModel(EvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    public IReadOnlyList<EvaluationQuestion> Questions { get; set; } = [];
    public IReadOnlyList<EvaluationResult> Results { get; set; } = [];
    public bool FineTuneConfigured { get; set; }
    public bool GeminiConfigured { get; set; }
    public IReadOnlyList<string> ChunkingStrategies { get; set; } = [];
    public IReadOnlyList<string> EmbeddingModels { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var data = await _evaluationService.GetDashboardDataAsync(cancellationToken);
        Questions = data.Questions;
        Results = data.Results;
        FineTuneConfigured = _evaluationService.FineTuneConfigured;
        GeminiConfigured = _evaluationService.GeminiConfigured;
        ChunkingStrategies = _evaluationService.AvailableChunkingStrategies;
        EmbeddingModels = _evaluationService.AvailableEmbeddingModels;
    }
}
