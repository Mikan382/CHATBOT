using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IDocumentEmbeddingRepository
{
    Task ReplaceEmbeddingsAsync(IReadOnlyList<DocumentChunkEmbedding> embeddings, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunkEmbedding>> ListByModelWithChunksAsync(string modelName, Guid? courseId, CancellationToken cancellationToken);
}
