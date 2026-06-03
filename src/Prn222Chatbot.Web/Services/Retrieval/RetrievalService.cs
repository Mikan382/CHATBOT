using System.Text.Json;
using Prn222Chatbot.Web.Repositories;
using Prn222Chatbot.Web.Services;
using Prn222Chatbot.Web.Services.Ai;

namespace Prn222Chatbot.Web.Services.Retrieval;

public class RetrievalService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentEmbeddingRepository _embeddingRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        IDocumentRepository documentRepository,
        IDocumentEmbeddingRepository embeddingRepository,
        IEmbeddingClient embeddingClient,
        ILogger<RetrievalService> logger)
    {
        _documentRepository = documentRepository;
        _embeddingRepository = embeddingRepository;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (_embeddingClient.IsConfigured)
        {
            try
            {
                var embeddedResults = await RetrieveWithEmbeddingsAsync(query, topK, cancellationToken);
                if (embeddedResults.Count > 0)
                {
                    return embeddedResults;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Embedding retrieval failed. Falling back to lexical retrieval.");
            }
        }

        return await RetrieveLexicalAsync(query, topK, cancellationToken);
    }

    private async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveWithEmbeddingsAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var queryVector = await _embeddingClient.EmbedQueryAsync(query, cancellationToken);
        var embeddings = await _embeddingRepository.ListByModelWithChunksAsync(_embeddingClient.ModelName, cancellationToken);
        if (embeddings.Count == 0)
        {
            return [];
        }

        return embeddings
            .Select(embedding => new
            {
                Embedding = embedding,
                Vector = JsonSerializer.Deserialize<float[]>(embedding.VectorJson) ?? []
            })
            .Select(x => new
            {
                x.Embedding,
                Score = Cosine(queryVector, x.Vector)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x =>
            {
                var chunk = x.Embedding.DocumentChunk!;
                return new RetrievedChunkDto(
                    chunk.Id,
                    chunk.DocumentId,
                    chunk.SourceName,
                    chunk.Document?.Chapter?.Title ?? "PRN222",
                    chunk.ChunkIndex,
                    chunk.Content,
                    x.Score);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveLexicalAsync(string query, int topK, CancellationToken cancellationToken)
    {
        var terms = TextNormalizer.Terms(query);
        if (terms.Count == 0)
        {
            return [];
        }

        var chunks = await _documentRepository.ListIndexedChunksAsync(cancellationToken);
        var scored = chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = Score(terms, chunk.NormalizedContent)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunkDto(
                x.Chunk.Id,
                x.Chunk.DocumentId,
                x.Chunk.SourceName,
                x.Chunk.Document?.Chapter?.Title ?? "PRN222",
                x.Chunk.ChunkIndex,
                x.Chunk.Content,
                x.Score))
            .ToList();

        return scored;
    }

    private static double Cosine(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var dimensions = Math.Min(left.Count, right.Count);
        if (dimensions == 0)
        {
            return 0;
        }

        var dot = 0d;
        var leftMagnitude = 0d;
        var rightMagnitude = 0d;
        for (var i = 0; i < dimensions; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude <= 0 || rightMagnitude <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static double Score(IReadOnlyList<string> terms, string normalizedContent)
    {
        var contentTerms = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (contentTerms.Length == 0)
        {
            return 0;
        }

        var matches = 0d;
        foreach (var term in terms.Distinct())
        {
            var exact = contentTerms.Count(x => x == term);
            if (exact > 0)
            {
                matches += exact * 2;
            }
            else if (normalizedContent.Contains(term, StringComparison.Ordinal))
            {
                matches += 0.5;
            }
        }

        return matches / (Math.Sqrt(terms.Count) * Math.Sqrt(contentTerms.Length) + 1);
    }
}
