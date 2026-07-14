using System.Text.Json;
using DataAccessLayer.Entities;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;
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
        var strategy = await _chunkingSettingsService.GetCurrentStrategyAsync(cancellationToken);
        if (!_chunkers.TryGetValue(strategy, out var chunker))
        {
            throw new InvalidOperationException("Configured chunking strategy is unavailable.");
        }

        var chunks = chunker.Chunk(document.ContentText)
            .Select((content, index) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkIndex = index + 1,
                Content = content,
                NormalizedContent = TextNormalizer.Normalize(content),
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

        if (_embeddingClient.IsConfigured)
        {
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
        }

        foreach (var chunk in chunks)
        {
            document.Chunks.Add(chunk);
        }
    }
}
