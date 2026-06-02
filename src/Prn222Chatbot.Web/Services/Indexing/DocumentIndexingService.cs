using Prn222Chatbot.Web.Domain;
using Prn222Chatbot.Web.Domain.Enums;
using Prn222Chatbot.Web.Repositories;
using Prn222Chatbot.Web.Services.Retrieval;

namespace Prn222Chatbot.Web.Services.Indexing;

public class DocumentIndexingService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly TextChunker _chunker;
    private readonly ILogger<DocumentIndexingService> _logger;

    public DocumentIndexingService(IDocumentRepository documentRepository, TextChunker chunker, ILogger<DocumentIndexingService> logger)
    {
        _documentRepository = documentRepository;
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
}
