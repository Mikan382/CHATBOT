using System.Diagnostics;
using System.Text.Json;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Services;

public class EvaluationService
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

    public async Task<(IReadOnlyList<EvaluationQuestion> Questions, IReadOnlyList<EvaluationResult> Results)> GetDashboardDataAsync(CancellationToken cancellationToken)
    {
        var questions = await _evaluationRepository.ListQuestionsAsync(cancellationToken);
        var results = await _evaluationRepository.ListRecentResultsAsync(200, cancellationToken);
        return (questions, results);
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
    public async Task<IReadOnlyList<EvaluationResult>> RunAsync(int limit, string? chunkingStrategy, string? embeddingModel, CancellationToken cancellationToken)
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
        return results;
    }

    /// <summary>
    /// Run a full comparative benchmark: all combinations of chunking strategies × embedding models.
    /// Builds the in-memory index once per (strategy, model) pair so each combination is truly distinct.
    /// </summary>
    public async Task<IReadOnlyList<EvaluationResult>> RunFullBenchmarkAsync(int questionLimit, CancellationToken cancellationToken)
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
        return allResults;
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

            if (chunks.Count == 0)
            {
                result.RagAnswer = "No relevant context was found in the indexed documents.";
            }
            else
            {
                var prompt = RagPromptBuilder.BuildPrompt(question.Question, chunks, []);
                result.RagAnswer = await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
            }
            ragStopwatch.Stop();
            result.RagLatencyMs = (int)ragStopwatch.ElapsedMilliseconds;

            // --- Fine-tuned evaluation (optional endpoint) ---
            if (_fineTuneClient.IsConfigured)
            {
                var ftStopwatch = Stopwatch.StartNew();
                var ft = await _fineTuneClient.GenerateAsync(
                    new FineTuneRequest(Guid.NewGuid().ToString(), "PRN222", question.Question, []),
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
}
