using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public interface IDocumentEmbeddingRepository
{
    Task ReplaceEmbeddingsAsync(IReadOnlyList<DocumentChunkEmbedding> embeddings, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, Guid? courseId, CancellationToken cancellationToken);
}

public class DocumentEmbeddingRepository : IDocumentEmbeddingRepository
{
    private readonly AppDbContext _db;

    public DocumentEmbeddingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task ReplaceEmbeddingsAsync(IReadOnlyList<DocumentChunkEmbedding> embeddings, CancellationToken cancellationToken)
    {
        if (embeddings.Count == 0)
        {
            return;
        }

        var chunkIds = embeddings.Select(x => x.DocumentChunkId).Distinct().ToList();
        var modelNames = embeddings.Select(x => x.ModelName).Distinct().ToList();

        await _db.DocumentChunkEmbeddings
            .Where(x => chunkIds.Contains(x.DocumentChunkId) && modelNames.Contains(x.ModelName))
            .ExecuteDeleteAsync(cancellationToken);

        _db.DocumentChunkEmbeddings.AddRange(embeddings);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, Guid? courseId, CancellationToken cancellationToken)
    {
        var query = _db.DocumentChunkEmbeddings
            .Include(x => x.DocumentChunk)
            .ThenInclude(x => x!.Document)
            .ThenInclude(x => x!.Chapter)
            .ThenInclude(x => x!.Course)
            .Where(x => x.ModelName == modelName && x.DocumentChunk!.Document!.IndexStatus == DocumentIndexStatus.Indexed);

        if (courseId.HasValue)
        {
            query = query.Where(x => x.DocumentChunk!.Document!.Chapter!.CourseId == courseId.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
