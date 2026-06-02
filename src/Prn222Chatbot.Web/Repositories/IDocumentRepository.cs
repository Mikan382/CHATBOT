using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Data;
using Prn222Chatbot.Web.Domain;
using Prn222Chatbot.Web.Domain.Enums;

namespace Prn222Chatbot.Web.Repositories;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> ListWithChapterAndChunksAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunk>> ListChunksAsync(Guid documentId, CancellationToken cancellationToken);
    Task<Document?> GetForIndexingAsync(Guid documentId, CancellationToken cancellationToken);
    Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListPendingOrProcessingIdsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunk>> ListIndexedChunksAsync(CancellationToken cancellationToken);
}

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;

    public DocumentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> ListWithChapterAndChunksAsync(CancellationToken cancellationToken)
    {
        return await _db.Documents
            .Include(x => x.Chapter)
            .Include(x => x.Chunks)
            .OrderByDescending(x => x.UploadedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> ListChunksAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _db.DocumentChunks
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.ChunkIndex)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Document?> GetForIndexingAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _db.Documents
            .Include(x => x.Chapter)
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
    }

    public async Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        await _db.DocumentChunks
            .Where(x => x.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);

        _db.DocumentChunks.AddRange(chunks);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListPendingOrProcessingIdsAsync(CancellationToken cancellationToken)
    {
        return await _db.Documents
            .Where(x => x.IndexStatus == DocumentIndexStatus.Pending || x.IndexStatus == DocumentIndexStatus.Processing)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> ListIndexedChunksAsync(CancellationToken cancellationToken)
    {
        return await _db.DocumentChunks
            .Include(x => x.Document)
            .ThenInclude(x => x!.Chapter)
            .Where(x => x.Document!.IndexStatus == DocumentIndexStatus.Indexed)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
