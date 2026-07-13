using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IChapterRepository
{
    Task<IReadOnlyList<Chapter>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken);
    Task<Chapter?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Chapter chapter, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> OrderExistsAsync(Guid courseId, int order, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> HasDependenciesAsync(Guid chapterId, CancellationToken cancellationToken);
}
