using System.Text.Json;
using DataAccessLayer.Entities;
using BusinessLayer.AI;
using BusinessLayer.Services;

namespace BusinessLayer.Indexing;

public class DocumentIndexingService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IChunkingSettingsService _chunkingSettingsService;
    private readonly IReadOnlyDictionary<string, ITextChunker> _chunkers;

    public DocumentIndexingService(
        IEmbeddingClient embeddingClient,
        IChunkingSettingsService chunkingSettingsService,
        IEnumerable<ITextChunker> chunkers)
    {
        _embeddingClient = embeddingClient;
        _chunkingSettingsService = chunkingSettingsService;
        _chunkers = chunkers
            .GroupBy(x => x.StrategyName)
            .ToDictionary(x => x.Key, x => x.First());
    }

    public async Task PopulateChunksAsync(Document document, CancellationToken cancellationToken)
    {
        if (!_embeddingClient.IsConfigured)
        {
            throw new InvalidOperationException("Hugging Face embedding is required to index documents.");
        }

        var settings = await _chunkingSettingsService.GetAsync(cancellationToken);
        var chunker = settings.CurrentStrategy == "fixed"
            ? new FixedSizeChunker(settings.FixedChunkSize, settings.FixedChunkOverlap)
            : _chunkers.GetValueOrDefault(settings.CurrentStrategy);
        if (chunker is null)
        {
            throw new InvalidOperationException("Configured chunking strategy is unavailable.");
        }

        document.ChunkingStrategy = settings.CurrentStrategy;

        var chunks = chunker.Chunk(document.ContentText)
            .Select((content, index) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkIndex = index + 1,
                Content = content,
                SourceName = document.OriginalFileName,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Could not split the document into searchable sections.");
        }

        if (chunks.Count > DocumentUploadLimits.MaxChunksPerDocument)
        {
            throw new InvalidOperationException(
                $"Document creates {chunks.Count} sections; the limit is {DocumentUploadLimits.MaxChunksPerDocument}.");
        }

        foreach (var chunk in chunks)
        {
            var vector = await _embeddingClient.EmbedPassageAsync(chunk.Content, cancellationToken);
            chunk.Embeddings.Add(new DocumentChunkEmbedding
            {
                Id = Guid.NewGuid(),
                DocumentChunkId = chunk.Id,
                ModelName = _embeddingClient.ModelName,
                Dimensions = vector.Length,
                VectorJson = JsonSerializer.Serialize(vector),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        foreach (var chunk in chunks)
        {
            document.Chunks.Add(chunk);
        }
    }

    public async Task PopulateStudentChunksAsync(StudentUploadedDocument document, CancellationToken cancellationToken)
    {
        if (!_embeddingClient.IsConfigured)
        {
            throw new InvalidOperationException("Hugging Face embedding is required to index documents.");
        }

        var settings = await _chunkingSettingsService.GetAsync(cancellationToken);
        var chunker = settings.CurrentStrategy == "fixed"
            ? new FixedSizeChunker(settings.FixedChunkSize, settings.FixedChunkOverlap)
            : _chunkers.GetValueOrDefault(settings.CurrentStrategy);
        if (chunker is null)
        {
            throw new InvalidOperationException("Configured chunking strategy is unavailable.");
        }

        document.ChunkingStrategy = settings.CurrentStrategy;

        var chunks = chunker.Chunk(document.ContentText)
            .Select((content, index) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                StudentDocumentId = document.Id,
                ChunkIndex = index + 1,
                Content = content,
                SourceName = document.OriginalFileName,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Could not split the document into searchable sections.");
        }

        if (chunks.Count > DocumentUploadLimits.MaxChunksPerDocument)
        {
            throw new InvalidOperationException(
                $"Document creates {chunks.Count} sections; the limit is {DocumentUploadLimits.MaxChunksPerDocument}.");
        }

        foreach (var chunk in chunks)
        {
            var vector = await _embeddingClient.EmbedPassageAsync(chunk.Content, cancellationToken);
            chunk.Embeddings.Add(new DocumentChunkEmbedding
            {
                Id = Guid.NewGuid(),
                DocumentChunkId = chunk.Id,
                ModelName = _embeddingClient.ModelName,
                Dimensions = vector.Length,
                VectorJson = JsonSerializer.Serialize(vector),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        foreach (var chunk in chunks)
        {
            document.Chunks.Add(chunk);
        }
    }
}
