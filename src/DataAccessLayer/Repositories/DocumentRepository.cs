using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

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
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException("This document content already exists in the selected chapter.", ex);
        }
    }

    public async Task<IReadOnlyList<Document>> ListWithChapterAndChunksAsync(string? searchTerm, Guid? courseId, Guid? chapterId, Guid? teacherId, CancellationToken cancellationToken)
    {
        var query = _db.Documents
            .Include(x => x.Chapter)
            .ThenInclude(x => x!.Course)
            .AsQueryable();

        if (teacherId.HasValue)
        {
            query = query.Where(x => x.Chapter!.Course!.TeacherAssignments.Any(t => t.TeacherUserId == teacherId.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            query = query.Where(x => x.OriginalFileName.Contains(normalizedSearchTerm));
        }

        if (chapterId.HasValue)
        {
            query = query.Where(x => x.ChapterId == chapterId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(x => x.Chapter!.CourseId == courseId.Value);
        }

        var docs = await query
            .OrderByDescending(x => x.UploadedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var docIds = docs.Select(d => d.Id).ToList();
        var chunkCounts = await _db.DocumentChunks
            .Where(c => docIds.Contains(c.DocumentId))
            .GroupBy(c => c.DocumentId)
            .Select(g => new { DocumentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DocumentId, x => x.Count, cancellationToken);

        foreach (var doc in docs)
        {
            if (chunkCounts.TryGetValue(doc.Id, out var count))
            {
                doc.ChunksCount = count;
            }
        }

        return docs;
    }

    public async Task<Document?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Documents
            .Include(x => x.Chapter)
            .ThenInclude(x => x!.Course)
            .Include(x => x.UploadedByUser)
            .Include(x => x.Chunks.OrderBy(c => c.ChunkIndex))
            .ThenInclude(x => x.Embeddings)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ContentHashExistsAsync(Guid chapterId, string contentHash, CancellationToken cancellationToken)
    {
        return await _db.Documents.AnyAsync(
            x => x.ChapterId == chapterId && x.ContentHash == contentHash,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null)
        {
            return false;
        }

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DocumentChunk>> ListIndexedChunksAsync(Guid? courseId, CancellationToken cancellationToken)
    {
        var query = _db.DocumentChunks
            .Include(x => x.Document)
            .ThenInclude(x => x!.Chapter)
            .ThenInclude(x => x!.Course)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(x => x.Document!.Chapter!.CourseId == courseId.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
