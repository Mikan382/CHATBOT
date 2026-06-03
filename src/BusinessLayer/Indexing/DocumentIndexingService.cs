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

        document.IndexStatus = DocumentIndexStatus.Processing;
        document.IndexError = null;
        await _documentRepository.SaveChangesAsync(cancellationToken);

        try
        {
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

            await _documentRepository.ReplaceChunksAsync(document.Id, chunks, cancellationToken);
            await _documentRepository.SaveChangesAsync(cancellationToken);
            await TryCreateEmbeddingsAsync(chunks, cancellationToken);

            document.IndexStatus = DocumentIndexStatus.Indexed;
            document.IndexError = null;
            await _documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            document.IndexStatus = DocumentIndexStatus.Failed;
            document.IndexError = ex.Message;
            await _documentRepository.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed indexing document {DocumentId}", documentId);
        }
    }

    private async Task TryCreateEmbeddingsAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        if (!_embeddingClient.IsConfigured || chunks.Count == 0)
        {
            return;
        }

        try
        {
            var embeddings = new List<DocumentChunkEmbedding>();
            foreach (var chunk in chunks)
            {
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
            }

            await _embeddingRepository.ReplaceEmbeddingsAsync(embeddings, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Document chunks were indexed, but embedding generation failed.");
        }
    }
}
