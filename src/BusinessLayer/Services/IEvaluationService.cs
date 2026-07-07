namespace BusinessLayer.Services;

public interface IEvaluationService
{
    bool GeminiConfigured { get; }
    bool FineTuneConfigured { get; }
    IReadOnlyList<string> AvailableChunkingStrategies { get; }
    IReadOnlyList<string> AvailableEmbeddingModels { get; }
    Task<BenchmarkDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EvaluationResultApiDto>> ListResultsAsync(CancellationToken cancellationToken);
    Task<int> RunAsync(int limit, string? chunkingStrategy, string? embeddingModel, CancellationToken cancellationToken);
    Task<int> RunFullBenchmarkAsync(int questionLimit, CancellationToken cancellationToken);
    Task RunFullBenchmarkWithProgressAsync(int questionLimit, Action onQuestionDone, CancellationToken cancellationToken);
}
