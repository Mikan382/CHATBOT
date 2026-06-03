using System.Text.Json;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Services;

public class EvaluationService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly RetrievalService _retrievalService;
    private readonly IGeminiClient _geminiClient;
    private readonly IFineTuneClient _fineTuneClient;

    public EvaluationService(
        IEvaluationRepository evaluationRepository,
        RetrievalService retrievalService,
        IGeminiClient geminiClient,
        IFineTuneClient fineTuneClient)
    {
        _evaluationRepository = evaluationRepository;
        _retrievalService = retrievalService;
        _geminiClient = geminiClient;
        _fineTuneClient = fineTuneClient;
    }

    public bool GeminiConfigured => _geminiClient.IsConfigured;

    public bool FineTuneConfigured => _fineTuneClient.IsConfigured;

    public async Task<(IReadOnlyList<EvaluationQuestion> Questions, IReadOnlyList<EvaluationResult> Results)> GetDashboardDataAsync(CancellationToken cancellationToken)
    {
        var questions = await _evaluationRepository.ListQuestionsAsync(cancellationToken);
        var results = await _evaluationRepository.ListRecentResultsAsync(20, cancellationToken);
        return (questions, results);
    }

    public async Task<IReadOnlyList<EvaluationResultApiDto>> ListResultsAsync(CancellationToken cancellationToken)
    {
        var results = await _evaluationRepository.ListRecentResultsAsync(50, cancellationToken);
        return results.Select(x => new EvaluationResultApiDto(
            x.Id,
            x.EvaluationQuestion?.Question ?? "",
            x.Status,
            x.Faithfulness,
            x.AnswerRelevance,
            x.RetrievalRecall,
            x.CitationAccuracy,
            x.ErrorMessage,
            x.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<EvaluationResult>> RunAsync(int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 5);
        var questions = await _evaluationRepository.ListQuestionsForRunAsync(limit, cancellationToken);

        var results = new List<EvaluationResult>();
        foreach (var question in questions)
        {
            var chunks = await _retrievalService.RetrieveAsync(question.Question, 3, cancellationToken);
            var result = new EvaluationResult
            {
                Id = Guid.NewGuid(),
                EvaluationQuestionId = question.Id,
                RetrievedChunksJson = JsonSerializer.Serialize(chunks),
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                if (chunks.Count == 0)
                {
                    result.RagAnswer = "No relevant context was found in the indexed documents.";
                }
                else
                {
                    var prompt = RagPromptBuilder.BuildPrompt(question.Question, chunks, []);
                    result.RagAnswer = await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
                }

                if (_fineTuneClient.IsConfigured)
                {
                    var ft = await _fineTuneClient.GenerateAsync(new FineTuneRequest(Guid.NewGuid().ToString(), "PRN222", question.Question, []), cancellationToken);
                    result.FineTunedAnswer = ft.Answer;
                }

                var score = Score(question.GroundTruth, result.RagAnswer, chunks);
                result.Faithfulness = score.Faithfulness;
                result.AnswerRelevance = score.AnswerRelevance;
                result.RetrievalRecall = score.RetrievalRecall;
                result.CitationAccuracy = score.CitationAccuracy;
                result.Status = "Completed";
            }
            catch (Exception ex)
            {
                result.Status = "Failed";
                result.ErrorMessage = ex.Message;
                result.RagAnswer = result.RagAnswer.Length == 0 ? "Evaluation failed." : result.RagAnswer;
            }

            results.Add(result);
        }

        await _evaluationRepository.SaveResultsAsync(results, cancellationToken);
        return results;
    }

    private static EvaluationScore Score(string groundTruth, string answer, IReadOnlyList<RetrievedChunkDto> chunks)
    {
        var truthTerms = TextNormalizer.Terms(groundTruth).Distinct().ToArray();
        var answerTerms = TextNormalizer.Terms(answer).ToHashSet();
        var contextTerms = TextNormalizer.Terms(string.Join(" ", chunks.Select(x => x.Content))).ToHashSet();

        decimal overlapWithAnswer = truthTerms.Length == 0 ? 0 : (decimal)truthTerms.Count(answerTerms.Contains) / truthTerms.Length;
        decimal overlapWithContext = truthTerms.Length == 0 ? 0 : (decimal)truthTerms.Count(contextTerms.Contains) / truthTerms.Length;
        decimal citation = answer.Contains("[Source", StringComparison.OrdinalIgnoreCase) || chunks.Count > 0 ? 1m : 0m;

        return new EvaluationScore(
            Math.Clamp(overlapWithContext, 0m, 1m),
            Math.Clamp(overlapWithAnswer, 0m, 1m),
            Math.Clamp(overlapWithContext, 0m, 1m),
            citation);
    }
}
