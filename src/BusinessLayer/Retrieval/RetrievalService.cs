using System.Text.Json;
using Microsoft.Extensions.Options;
using BusinessLayer.AI;
using BusinessLayer.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Retrieval;

public class RetrievalService
{
    private readonly IDocumentEmbeddingRepository _embeddingRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly RagOptions _options;

    public RetrievalService(
        IDocumentEmbeddingRepository embeddingRepository,
        IEmbeddingClient embeddingClient,
        IOptions<RagOptions> options)
    {
        _embeddingRepository = embeddingRepository;
        _embeddingClient = embeddingClient;
        _options = options.Value;
    }

    public bool IsConfigured => _embeddingClient.IsConfigured;

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveAsync(
        string query,
        Guid? courseId,
        CancellationToken cancellationToken)
    {
        if (!_embeddingClient.IsConfigured)
        {
            throw new InvalidOperationException("Hugging Face embedding is not configured.");
        }

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
                Vector = ReadStoredVector(
                    embedding.Id,
                    embedding.VectorJson,
                    embedding.Dimensions,
                    queryVector.Length)
            })
            .Select(x => new
            {
                x.Embedding,
                Score = CosineSimilarity.Cosine(queryVector.AsSpan(), x.Vector.AsSpan())
            })
            .Where(x => x.Score >= _options.MinimumSimilarityScore)
            .OrderByDescending(x => x.Score)
            .Take(_options.TopK)
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

    private static float[] ReadStoredVector(
        Guid embeddingId,
        string vectorJson,
        int storedDimensions,
        int expectedDimensions)
    {
        float[]? vector;
        try
        {
            vector = JsonSerializer.Deserialize<float[]>(vectorJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Stored embedding '{embeddingId}' contains invalid vector data. Re-index its document.",
                exception);
        }

        if (vector is null
            || vector.Length == 0
            || vector.Length != storedDimensions
            || storedDimensions != expectedDimensions
            || vector.Any(value => !float.IsFinite(value)))
        {
            throw new InvalidOperationException(
                $"Stored embedding '{embeddingId}' has invalid dimensions or values. Re-index its document.");
        }

        return vector;
    }
}
