using BusinessLayer.Services;

namespace PresentationLayer.ViewModels;

public class BenchmarkIndexViewModel
{
    public BenchmarkDashboardDto Dashboard { get; set; } = new([], [], [], [], []);
    public bool FineTuneConfigured { get; set; }
    public bool GeminiConfigured { get; set; }
    public IReadOnlyList<string> ChunkingStrategies { get; set; } = [];
    public IReadOnlyList<string> EmbeddingModels { get; set; } = [];
}
