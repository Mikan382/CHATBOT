using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> ListWithChapterAndChunksAsync(string? searchTerm, Guid? courseId, Guid? chapterId, Guid? teacherId, CancellationToken cancellationToken);
    Task<Document?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ContentHashExistsAsync(Guid chapterId, string contentHash, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunk>> ListIndexedChunksAsync(Guid? courseId, CancellationToken cancellationToken);
}
