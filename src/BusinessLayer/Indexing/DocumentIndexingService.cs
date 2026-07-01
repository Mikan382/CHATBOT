using System.Text.Json;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Indexing;

public class DocumentIndexingService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentEmbeddingRepository _embeddingRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly TextChunker _chunker;
    private readonly ILogger<DocumentIndexingService> _logger;

    public DocumentIndexingService(
        IDocumentRepository documentRepository,
        IDocumentEmbeddingRepository embeddingRepository,
        IEmbeddingClient embeddingClient,
        TextChunker chunker,
        ILogger<DocumentIndexingService> logger)
    {
        _documentRepository = documentRepository;
        _embeddingRepository = embeddingRepository;
        _embeddingClient = embeddingClient;
        _chunker = chunker;
        _logger = logger;
    }

    public async Task IndexAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetForIndexingAsync(documentId, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Skipping indexing for missing document {DocumentId}", documentId);
            return;
        }

        await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 10, "Preparing document", null, cancellationToken);

        try
        {
            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 30, "Chunking text", null, cancellationToken);
            var chunks = _chunker.Chunk(document.ContentText)
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

            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 50, "Saving chunks", null, cancellationToken);
            await _documentRepository.ReplaceChunksAsync(document.Id, chunks, cancellationToken);
            await _documentRepository.SaveChangesAsync(cancellationToken);
            var embeddingStage = await TryCreateEmbeddingsAsync(document, chunks, cancellationToken);

            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 98, "Finalizing", null, cancellationToken);
            await UpdateProgressAsync(document, DocumentIndexStatus.Indexed, 100, embeddingStage, null, cancellationToken);
        }
        catch (Exception ex)
        {
            await UpdateProgressAsync(document, DocumentIndexStatus.Failed, document.IndexProgressPercent, "Failed", ex.Message, cancellationToken);
            _logger.LogError(ex, "Failed indexing document {DocumentId}", documentId);
        }
    }

    private async Task<string> TryCreateEmbeddingsAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0)
        {
            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 95, "No chunks to embed", null, cancellationToken);
            return "Indexed without embeddings";
        }

        if (!_embeddingClient.IsConfigured)
        {
            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 95, "Embedding skipped", null, cancellationToken);
            return "Indexed without embeddings";
        }

        try
        {
            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 60, "Creating embeddings", null, cancellationToken);
            var embeddings = new List<DocumentChunkEmbedding>();
            for (var index = 0; index < chunks.Count; index++)
            {
                var chunk = chunks[index];
                var vector = await _embeddingClient.EmbedPassageAsync(chunk.Content, cancellationToken);
                embeddings.Add(new DocumentChunkEmbedding
                {
                    Id = Guid.NewGuid(),
                    DocumentChunkId = chunk.Id,
                    ModelName = _embeddingClient.ModelName,
                    Dimensions = vector.Length,
                    VectorJson = JsonSerializer.Serialize(vector),
                    CreatedAtUtc = DateTime.UtcNow
                });

                var progress = 60 + (int)Math.Floor(((index + 1) / (double)chunks.Count) * 35);
                await UpdateProgressAsync(document, DocumentIndexStatus.Processing, progress, $"Creating embeddings ({index + 1}/{chunks.Count})", null, cancellationToken);
            }

            await _embeddingRepository.ReplaceEmbeddingsAsync(embeddings, cancellationToken);
            return "Indexed with embeddings";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await UpdateProgressAsync(document, DocumentIndexStatus.Processing, 95, "Chunks indexed; embedding failed", null, cancellationToken);
            _logger.LogWarning(ex, "Document chunks were indexed, but embedding generation failed.");
            return "Indexed; embedding failed";
        }
    }

    private async Task UpdateProgressAsync(
        Document document,
        DocumentIndexStatus status,
        int percent,
        string stage,
        string? error,
        CancellationToken cancellationToken)
    {
        document.IndexStatus = status;
        document.IndexProgressPercent = Math.Clamp(percent, 0, 100);
        document.IndexStage = stage;
        document.IndexError = error;
        await _documentRepository.SaveChangesAsync(cancellationToken);
    }
}
