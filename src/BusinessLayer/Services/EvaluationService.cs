using System.Diagnostics;
using System.Text.Json;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Services;

public class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly BenchmarkRetrievalService _benchmarkRetrieval;
    private readonly IGeminiClient _geminiClient;
    private readonly IFineTuneClient _fineTuneClient;
    private readonly RagasScorer _ragasScorer;
    private readonly EmbeddingClientFactory _embeddingClientFactory;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(
        IEvaluationRepository evaluationRepository,
        BenchmarkRetrievalService benchmarkRetrieval,
        IGeminiClient geminiClient,
        IFineTuneClient fineTuneClient,
        RagasScorer ragasScorer,
        EmbeddingClientFactory embeddingClientFactory,
        ILogger<EvaluationService> logger)
    {
        _evaluationRepository = evaluationRepository;
        _benchmarkRetrieval = benchmarkRetrieval;
        _geminiClient = geminiClient;
        _fineTuneClient = fineTuneClient;
        _ragasScorer = ragasScorer;
        _embeddingClientFactory = embeddingClientFactory;
        _logger = logger;
    }

    public bool GeminiConfigured => _geminiClient.IsConfigured;

    public bool FineTuneConfigured => _fineTuneClient.IsConfigured;

    public IReadOnlyList<string> AvailableChunkingStrategies => ["paragraph", "fixed_1000", "sentence"];

    public IReadOnlyList<string> AvailableEmbeddingModels => _embeddingClientFactory.GetModelNames();

    public async Task<BenchmarkDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken)
    {
        var questions = await _evaluationRepository.ListQuestionsAsync(cancellationToken);
        var results = await _evaluationRepository.ListRecentResultsAsync(200, cancellationToken);

        var questionDtos = questions.Select(q => new EvaluationQuestionDto(q.Id, q.Question, q.GroundTruth)).ToList();
        var resultDtos = results.Select(ToViewDto).ToList();
        var completedResults = resultDtos.Where(r => r.Status == "Completed").ToList();

        // Aggregation logic moved from Benchmark view (fix #4)
        var byModel = completedResults
            .GroupBy(r => string.IsNullOrWhiteSpace(r.EmbeddingModelName) ? "default" : r.EmbeddingModelName)
            .Select(g => new BenchmarkAggregateRow(
                g.Key,
                g.Average(x => x.Faithfulness),
                g.Average(x => x.AnswerRelevance),
                g.Average(x => x.RetrievalRecall),
                g.Average(x => x.CitationAccuracy),
                g.Count()))
            .ToList();

        var byStrategy = completedResults
            .GroupBy(r => string.IsNullOrWhiteSpace(r.ChunkingStrategy) ? "paragraph" : r.ChunkingStrategy)
            .Select(g => new BenchmarkAggregateRow(
                g.Key,
                g.Average(x => x.Faithfulness),
                g.Average(x => x.AnswerRelevance),
                g.Average(x => x.RetrievalRecall),
                g.Average(x => x.CitationAccuracy),
                g.Count()))
            .ToList();

        var ragVsFt = completedResults
            .Where(r => !string.IsNullOrWhiteSpace(r.FineTunedAnswer))
            .ToList();

        return new BenchmarkDashboardDto(questionDtos, resultDtos, byModel, byStrategy, ragVsFt);
    }

    public async Task<IReadOnlyList<EvaluationResultApiDto>> ListResultsAsync(CancellationToken cancellationToken)
    {
        var results = await _evaluationRepository.ListRecentResultsAsync(200, cancellationToken);
        return results.Select(x => new EvaluationResultApiDto(
            x.Id,
            x.EvaluationQuestion?.Question ?? "",
            x.Status,
            x.Faithfulness,
            x.AnswerRelevance,
            x.RetrievalRecall,
            x.CitationAccuracy,
            x.ErrorMessage,
            x.CreatedAtUtc,
            x.ChunkingStrategy,
            x.EmbeddingModelName,
            x.RagLatencyMs,
            x.FineTunedLatencyMs,
            x.RagAnswer,
            x.FineTunedAnswer,
            x.FtFaithfulness,
            x.FtAnswerRelevance)).ToList();
    }

    /// <summary>
    /// Run a benchmark for a specific chunking strategy and embedding model combination.
    /// </summary>
    public async Task<int> RunAsync(int limit, string? chunkingStrategy, string? embeddingModel, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 50);
        var questions = await _evaluationRepository.ListQuestionsForRunAsync(limit, cancellationToken);
        var strategy = chunkingStrategy ?? "paragraph";
        var modelName = embeddingModel ?? _embeddingClientFactory.GetModelNames().FirstOrDefault() ?? "default";

        var index = await _benchmarkRetrieval.BuildIndexAsync(null, strategy, modelName, cancellationToken);
        var results = new List<EvaluationResult>();
        foreach (var question in questions)
        {
            results.Add(await EvaluateQuestionAsync(question, strategy, modelName, index, cancellationToken));
        }

        await _evaluationRepository.SaveResultsAsync(results, cancellationToken);
        return results.Count;
    }

    /// <summary>
    /// Run a full comparative benchmark: all combinations of chunking strategies × embedding models.
    /// Builds the in-memory index once per (strategy, model) pair so each combination is truly distinct.
    /// </summary>
    public async Task<int> RunFullBenchmarkAsync(int questionLimit, CancellationToken cancellationToken)
    {
        questionLimit = Math.Clamp(questionLimit, 1, 50);
        var questions = await _evaluationRepository.ListQuestionsForRunAsync(questionLimit, cancellationToken);
        var strategies = AvailableChunkingStrategies;
        var models = AvailableEmbeddingModels;

        var allResults = new List<EvaluationResult>();
        foreach (var strategy in strategies)
        {
            foreach (var model in models)
            {
                var index = await _benchmarkRetrieval.BuildIndexAsync(null, strategy, model, cancellationToken);
                foreach (var question in questions)
                {
                    allResults.Add(await EvaluateQuestionAsync(question, strategy, model, index, cancellationToken));
                }
            }
        }

        await _evaluationRepository.SaveResultsAsync(allResults, cancellationToken);
        return allResults.Count;
    }

    /// <summary>
    /// Same as RunFullBenchmarkAsync but calls onQuestionDone after each question for progress reporting.
    /// </summary>
    public async Task RunFullBenchmarkWithProgressAsync(int questionLimit, Action onQuestionDone, CancellationToken cancellationToken)
    {
        questionLimit = Math.Clamp(questionLimit, 1, 50);
        var questions = await _evaluationRepository.ListQuestionsForRunAsync(questionLimit, cancellationToken);
        var strategies = AvailableChunkingStrategies;
        var models = AvailableEmbeddingModels;

        var allResults = new List<EvaluationResult>();
        foreach (var strategy in strategies)
        {
            foreach (var model in models)
            {
                var index = await _benchmarkRetrieval.BuildIndexAsync(null, strategy, model, cancellationToken);
                foreach (var question in questions)
                {
                    allResults.Add(await EvaluateQuestionAsync(question, strategy, model, index, cancellationToken));
                    onQuestionDone();
                }
            }
        }

        await _evaluationRepository.SaveResultsAsync(allResults, cancellationToken);
    }

    private async Task<EvaluationResult> EvaluateQuestionAsync(
        EvaluationQuestion question,
        string chunkingStrategy,
        string embeddingModelName,
        BenchmarkIndex index,
        CancellationToken cancellationToken)
    {
        var result = new EvaluationResult
        {
            Id = Guid.NewGuid(),
            EvaluationQuestionId = question.Id,
            ChunkingStrategy = chunkingStrategy,
            EmbeddingModelName = embeddingModelName,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Honest signal: model not configured → Skipped, NOT silent fallback
        if (!index.Available)
        {
            result.Status = "Skipped";
            result.ErrorMessage = index.UnavailableReason;
            result.RagAnswer = index.UnavailableReason ?? "Benchmark index unavailable.";
            return result;
        }

        try
        {
            // --- RAG evaluation ---
            var ragStopwatch = Stopwatch.StartNew();
            var chunks = await index.RetrieveAsync(question.Question, 3, cancellationToken);
            result.RetrievedChunksJson = JsonSerializer.Serialize(chunks);

            var courseName = question.Chapter?.Course?.Name ?? question.Chapter?.Course?.Code ?? "this course";
            if (chunks.Count == 0)
            {
                result.RagAnswer = "No relevant context was found in the indexed documents.";
            }
            else
            {
                var prompt = RagPromptBuilder.BuildPrompt(question.Question, chunks, []);
                result.RagAnswer = await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(courseName), prompt, cancellationToken);
            }
            ragStopwatch.Stop();
            result.RagLatencyMs = (int)ragStopwatch.ElapsedMilliseconds;

            // --- Fine-tuned evaluation (optional endpoint) ---
            if (_fineTuneClient.IsConfigured)
            {
                var ftStopwatch = Stopwatch.StartNew();
                var courseCode = question.Chapter?.Course?.Code ?? "unknown";
                var ft = await _fineTuneClient.GenerateAsync(
                    new FineTuneRequest(Guid.NewGuid().ToString(), courseCode, question.Question, []),
                    cancellationToken);
                result.FineTunedAnswer = ft.Answer;
                ftStopwatch.Stop();
                result.FineTunedLatencyMs = (int)ftStopwatch.ElapsedMilliseconds;
            }

            // --- RAGAS scoring for RAG ---
            var ragScore = await _ragasScorer.ScoreAsync(
                question.Question, question.GroundTruth, result.RagAnswer, chunks, cancellationToken);
            result.Faithfulness = ragScore.Faithfulness;
            result.AnswerRelevance = ragScore.AnswerRelevance;
            result.RetrievalRecall = ragScore.RetrievalRecall;
            result.CitationAccuracy = ragScore.CitationAccuracy;

            // --- RAGAS scoring for Fine-tuned (N2 fix: use same chunks as RAG for fair comparison) ---
            if (!string.IsNullOrWhiteSpace(result.FineTunedAnswer))
            {
                var ftScore = await _ragasScorer.ScoreAsync(
                    question.Question, question.GroundTruth, result.FineTunedAnswer, chunks, cancellationToken);
                result.FtFaithfulness = ftScore.Faithfulness;
                result.FtAnswerRelevance = ftScore.AnswerRelevance;
            }

            result.Status = "Completed";
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            if (string.IsNullOrWhiteSpace(result.RagAnswer))
            {
                result.RagAnswer = "Evaluation failed.";
            }

            _logger.LogWarning(ex, "Evaluation failed for question {QuestionId} strategy={Strategy} model={Model}",
                question.Id, chunkingStrategy, embeddingModelName);
        }

        return result;
    }

    private static EvaluationResultViewDto ToViewDto(EvaluationResult r)
    {
        return new EvaluationResultViewDto(
            r.Id,
            r.EvaluationQuestion?.Question,
            r.EvaluationQuestion?.GroundTruth,
            r.Status,
            r.Faithfulness,
            r.AnswerRelevance,
            r.RetrievalRecall,
            r.CitationAccuracy,
            r.ErrorMessage,
            r.CreatedAtUtc,
            r.ChunkingStrategy,
            r.EmbeddingModelName,
            r.RagLatencyMs,
            r.FineTunedLatencyMs,
            r.RagAnswer,
            r.FineTunedAnswer,
            r.FtFaithfulness,
            r.FtAnswerRelevance);
    }
}
