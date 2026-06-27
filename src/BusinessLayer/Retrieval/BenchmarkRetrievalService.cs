using System.Text.Json;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Indexing;
using BusinessLayer.Services;

namespace BusinessLayer.Retrieval;

/// <summary>
/// Builds an in-memory, fully comparable retrieval index for benchmarking.
/// Re-chunks document text with the requested strategy and re-embeds with the
/// requested embedding model. Nothing is persisted to the production DB.
/// </summary>
public class BenchmarkRetrievalService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly EmbeddingClientFactory _embeddingClientFactory;
    private readonly IEnumerable<ITextChunker> _chunkers;
    private readonly ILogger<BenchmarkRetrievalService> _logger;

    public BenchmarkRetrievalService(
        IDocumentRepository documentRepository,
        EmbeddingClientFactory embeddingClientFactory,
        IEnumerable<ITextChunker> chunkers,
        ILogger<BenchmarkRetrievalService> logger)
    {
        _documentRepository = documentRepository;
        _embeddingClientFactory = embeddingClientFactory;
        _chunkers = chunkers;
        _logger = logger;
    }

    public async Task<BenchmarkIndex> BuildIndexAsync(
        Guid? courseId,
        string strategyName,
        string modelName,
        CancellationToken cancellationToken)
    {
        var client = _embeddingClientFactory.GetByName(modelName);
        if (client is null || !client.IsConfigured)
        {
            return BenchmarkIndex.Unavailable($"Embedding model '{modelName}' is not configured. Set the API key via user-secrets.");
        }

        var chunker = _chunkers.FirstOrDefault(c => c.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
        if (chunker is null)
        {
            return BenchmarkIndex.Unavailable($"Chunking strategy '{strategyName}' is not registered.");
        }

        var documents = await _documentRepository.ListWithChapterAndChunksAsync(
            null, courseId, null, DocumentIndexStatus.Indexed, cancellationToken);

        if (documents.Count == 0)
        {
            return BenchmarkIndex.Unavailable("No indexed documents found. Upload and index documents first.");
        }

        var entries = new List<BenchmarkIndexEntry>();
        foreach (var doc in documents)
        {
            if (string.IsNullOrWhiteSpace(doc.ContentText))
            {
                continue;
            }

            var chunks = chunker.Chunk(doc.ContentText);
            var chapterTitle = doc.Chapter?.Title ?? "PRN222";
            var chunkIndex = 0;

            foreach (var chunkText in chunks)
            {
                chunkIndex++;
                try
                {
                    var vector = await client.EmbedPassageAsync(chunkText, cancellationToken);
                    entries.Add(new BenchmarkIndexEntry(
                        doc.Id,
                        doc.OriginalFileName,
                        chapterTitle,
                        chunkIndex,
                        chunkText,
                        vector));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to embed chunk {Index} of document {DocId} with model {Model}",
                        chunkIndex, doc.Id, modelName);
                }
            }
        }

        return new BenchmarkIndex(entries, client, strategyName, modelName);
    }
}

public class BenchmarkIndex
{
    private readonly IReadOnlyList<BenchmarkIndexEntry> _entries;
    private readonly IEmbeddingClient? _client;

    public bool Available { get; }
    public string? UnavailableReason { get; }
    public string StrategyName { get; }
    public string ModelName { get; }

    public BenchmarkIndex(IReadOnlyList<BenchmarkIndexEntry> entries, IEmbeddingClient client, string strategyName, string modelName)
    {
        _entries = entries;
        _client = client;
        Available = true;
        StrategyName = strategyName;
        ModelName = modelName;
    }

    private BenchmarkIndex(string reason)
    {
        _entries = [];
        Available = false;
        UnavailableReason = reason;
        StrategyName = "";
        ModelName = "";
    }

    public static BenchmarkIndex Unavailable(string reason) => new(reason);

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (!Available || _client is null || _entries.Count == 0)
        {
            return [];
        }

        var queryVector = await _client.EmbedQueryAsync(query, cancellationToken);

        return _entries
            .Select(e => new { Entry = e, Score = Cosine(queryVector, e.Vector) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunkDto(
                Guid.Empty,
                x.Entry.DocumentId,
                x.Entry.SourceName,
                x.Entry.ChapterTitle,
                x.Entry.ChunkIndex,
                x.Entry.Content,
                x.Score))
            .ToList();
    }

    private static double Cosine(float[] left, float[] right)
    {
        var dims = Math.Min(left.Length, right.Length);
        if (dims == 0)
        {
            return 0;
        }

        var dot = 0d;
        var lMag = 0d;
        var rMag = 0d;
        for (var i = 0; i < dims; i++)
        {
            dot += left[i] * right[i];
            lMag += left[i] * left[i];
            rMag += right[i] * right[i];
        }

        return lMag <= 0 || rMag <= 0 ? 0 : dot / (Math.Sqrt(lMag) * Math.Sqrt(rMag));
    }
}

public record BenchmarkIndexEntry(
    Guid DocumentId,
    string SourceName,
    string ChapterTitle,
    int ChunkIndex,
    string Content,
    float[] Vector);
