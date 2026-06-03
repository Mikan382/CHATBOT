using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public interface IDocumentEmbeddingRepository
{
    Task ReplaceEmbeddingsAsync(IReadOnlyList<DocumentChunkEmbedding> embeddings, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, CancellationToken cancellationToken);
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

    public async Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, CancellationToken cancellationToken)
    {
        return await _db.DocumentChunkEmbeddings
            .Include(x => x.DocumentChunk)
            .ThenInclude(x => x!.Document)
            .ThenInclude(x => x!.Chapter)
            .Where(x => x.ModelName == modelName && x.DocumentChunk!.Document!.IndexStatus == DocumentIndexStatus.Indexed)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
