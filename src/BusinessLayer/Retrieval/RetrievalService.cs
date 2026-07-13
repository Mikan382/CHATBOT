using System.Text.Json;
using Microsoft.Extensions.Logging;
using BusinessLayer.AI;
using BusinessLayer.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Retrieval;

public class RetrievalService
{
    private const double MinEmbeddingScore = 0.36;
    private const double MinLexicalScore = 0.02;
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

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveAsync(string query, Guid? courseId, int topK, CancellationToken cancellationToken)
    {
        if (_embeddingClient.IsConfigured)
        {
            try
            {
                var embeddedResults = await RetrieveWithEmbeddingsAsync(query, courseId, topK, cancellationToken);
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

        return await RetrieveLexicalAsync(query, courseId, topK, cancellationToken);
    }

    private async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveWithEmbeddingsAsync(
        string query,
        Guid? courseId,
        int topK,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var queryVector = await _embeddingClient.EmbedQueryAsync(query, cancellationToken);
        var embeddings = await _embeddingRepository.ListByModelWithChunksAsync(
            _embeddingClient.ModelName,
            courseId,
            cancellationToken);
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
                Score = CosineSimilarity.Cosine(queryVector.AsSpan(), x.Vector.AsSpan())
            })
            .Where(x => x.Score >= MinEmbeddingScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x =>
            {
                var chunk = x.Embedding.DocumentChunk!;
                return new RetrievedChunkDto(
                    chunk.Id,
                    chunk.DocumentId,
                    chunk.SourceName,
                    chunk.Document?.Chapter?.Title ?? "Unknown",
                    chunk.ChunkIndex,
                    chunk.Content,
                    x.Score);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveLexicalAsync(string query, Guid? courseId, int topK, CancellationToken cancellationToken)
    {
        var terms = TextNormalizer.Terms(query);
        if (terms.Count == 0)
        {
            return [];
        }

        var chunks = await _documentRepository.ListIndexedChunksAsync(courseId, cancellationToken);
        var scored = chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = Score(terms, chunk.NormalizedContent)
            })
            .Where(x => x.Score >= MinLexicalScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunkDto(
                x.Chunk.Id,
                x.Chunk.DocumentId,
                x.Chunk.SourceName,
                x.Chunk.Document?.Chapter?.Title ?? "Unknown",
                x.Chunk.ChunkIndex,
                x.Chunk.Content,
                x.Score))
            .ToList();

        return scored;
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
