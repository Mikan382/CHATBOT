using DataAccessLayer.Entities;

namespace PresentationLayer.ViewModels;

public class BenchmarkIndexViewModel
{
    public IReadOnlyList<EvaluationQuestion> Questions { get; set; } = [];
    public IReadOnlyList<EvaluationResult> Results { get; set; } = [];
    public bool FineTuneConfigured { get; set; }
    public bool GeminiConfigured { get; set; }
}
