using System.Text.Json;
using Microsoft.Extensions.Options;
using BusinessLayer.AI;
using BusinessLayer.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Retrieval;

public class RetrievalService
{
    private readonly IDocumentEmbeddingRepository _embeddingRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly RagOptions _options;

    public RetrievalService(
        IDocumentEmbeddingRepository embeddingRepository,
        ICourseRepository courseRepository,
        IEmbeddingClient embeddingClient,
        IOptions<RagOptions> options)
    {
        _embeddingRepository = embeddingRepository;
        _courseRepository = courseRepository;
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

        // Chunks are stored per embedding model, so the question must be embedded with the
        // model this course was indexed with - otherwise nothing matches.
        var modelName = await ResolveModelNameAsync(courseId, cancellationToken);
        var queryVector = await _embeddingClient.EmbedQueryAsync(modelName, query, cancellationToken);
        var embeddings = await _embeddingRepository.ListByModelWithChunksAsync(
            modelName,
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
                var docId = chunk.DocumentId ?? Guid.Empty;
                var chapterTitle = chunk.Document?.Chapter?.Title ?? "Unknown";
                return new RetrievedChunkDto(
                    chunk.Id,
                    docId,
                    chunk.SourceName,
                    chapterTitle,
                    chunk.ChunkIndex,
                    chunk.Content,
                    x.Score);
            })
            .ToList();
    }

    private async Task<string> ResolveModelNameAsync(Guid? courseId, CancellationToken cancellationToken)
    {
        if (!courseId.HasValue)
        {
            return _embeddingClient.ModelName;
        }

        var course = await _courseRepository.GetByIdAsync(courseId.Value, cancellationToken);
        var courseModel = course?.DefaultEmbeddingModel;
        return !string.IsNullOrWhiteSpace(courseModel) && _embeddingClient.IsModelConfigured(courseModel)
            ? courseModel
            : _embeddingClient.ModelName;
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
