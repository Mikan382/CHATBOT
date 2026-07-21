using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IDocumentEmbeddingRepository
{
    Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, Guid? courseId, CancellationToken cancellationToken);
}
