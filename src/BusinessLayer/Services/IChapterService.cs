namespace BusinessLayer.Services;

public interface IChapterService
{
    Task<IReadOnlyList<ChapterDto>> ListByCourseAsync(Guid courseId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<ChapterFormDto?> GetEditableAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<(Guid Id, Guid CourseId)> CreateAsync(Guid courseId, int order, string? clo, string title, string? summary, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, Guid courseId, int order, string? clo, string title, string? summary, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken);
}
