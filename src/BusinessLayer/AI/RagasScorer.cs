using System.Text.Json;

namespace BusinessLayer.AI;

/// <summary>
/// LLM-based RAGAS-style evaluation scorer. Uses Gemini to judge:
/// - Faithfulness: Is the answer grounded in the provided context?
/// - Answer Relevance: Does the answer address the question?
/// - Context Recall: Does the retrieved context cover the ground truth?
/// - Citation Accuracy: Are source citations present and correct?
/// </summary>
public class RagasScorer
{
    private readonly IGeminiClient _geminiClient;
    private readonly ILogger<RagasScorer> _logger;

    public RagasScorer(IGeminiClient geminiClient, ILogger<RagasScorer> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
    }

    public bool IsAvailable => _geminiClient.IsConfigured;

    public async Task<EvaluationScore> ScoreAsync(
        string question,
        string groundTruth,
        string answer,
        IReadOnlyList<RetrievedChunkDto> chunks,
        CancellationToken cancellationToken)
    {
        if (!_geminiClient.IsConfigured)
        {
            return FallbackLexicalScore(groundTruth, answer, chunks);
        }

        try
        {
            var context = string.Join("\n---\n", chunks.Select(c => c.Content));

            var prompt = $"""
            You are an evaluation judge. Score the following answer on 4 metrics from 0.0 to 1.0.
            Return ONLY a JSON object with exactly these fields: faithfulness, answer_relevance, context_recall, citation_accuracy.

            Question: {question}

            Ground Truth Answer: {groundTruth}

            Retrieved Context:
            {(string.IsNullOrWhiteSpace(context) ? "(no context retrieved)" : context)}

            Generated Answer: {answer}

            Scoring criteria:
            - faithfulness: Is the answer factually consistent with the retrieved context? (1.0 = fully grounded, 0.0 = contradicts or fabricates)
            - answer_relevance: Does the answer directly address the question? (1.0 = perfectly relevant, 0.0 = off-topic)
            - context_recall: Does the retrieved context contain the information from the ground truth? (1.0 = full coverage, 0.0 = no overlap)
            - citation_accuracy: Does the answer cite sources correctly? (1.0 = proper citations, 0.0 = no citations when sources exist)

            Respond with ONLY the JSON object, no explanation.
            """;

            var systemInstruction = "You are a strict evaluation judge for RAG systems. Output only valid JSON.";
            var responseText = await _geminiClient.GenerateAsync(systemInstruction, prompt, cancellationToken);

            return ParseScores(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM-based RAGAS scoring failed, falling back to lexical scoring.");
            return FallbackLexicalScore(groundTruth, answer, chunks);
        }
    }

    private static EvaluationScore ParseScores(string responseText)
    {
        // Extract JSON from potential markdown code blocks
        var json = responseText.Trim();
        if (json.StartsWith("```"))
        {
            var startIdx = json.IndexOf('{');
            var endIdx = json.LastIndexOf('}');
            if (startIdx >= 0 && endIdx > startIdx)
            {
                json = json[startIdx..(endIdx + 1)];
            }
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new EvaluationScore(
            GetDecimal(root, "faithfulness"),
            GetDecimal(root, "answer_relevance"),
            GetDecimal(root, "context_recall"),
            GetDecimal(root, "citation_accuracy"));
    }

    private static decimal GetDecimal(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            if (value.TryGetDecimal(out var d))
            {
                return Math.Clamp(d, 0m, 1m);
            }

            if (value.TryGetDouble(out var dbl))
            {
                return Math.Clamp((decimal)dbl, 0m, 1m);
            }
        }

        return 0m;
    }

    private static EvaluationScore FallbackLexicalScore(string groundTruth, string answer, IReadOnlyList<RetrievedChunkDto> chunks)
    {
        var truthTerms = BusinessLayer.Retrieval.TextNormalizer.Terms(groundTruth).Distinct().ToArray();
        var answerTerms = BusinessLayer.Retrieval.TextNormalizer.Terms(answer).ToHashSet();
        var contextTerms = BusinessLayer.Retrieval.TextNormalizer.Terms(string.Join(" ", chunks.Select(x => x.Content))).ToHashSet();

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
