using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public class DocumentEmbeddingRepository : IDocumentEmbeddingRepository
{
    private readonly AppDbContext _db;

    public DocumentEmbeddingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, Guid? courseId, CancellationToken cancellationToken)
    {
        var query = _db.DocumentChunkEmbeddings
            .Include(x => x.DocumentChunk)
                .ThenInclude(x => x!.Document)
                .ThenInclude(x => x!.Chapter)
                .ThenInclude(x => x!.Course)
            .Where(x => x.ModelName == modelName);

        if (courseId.HasValue)
        {
            query = query.Where(x => x.DocumentChunk!.Document != null && x.DocumentChunk.Document.Chapter!.CourseId == courseId.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
