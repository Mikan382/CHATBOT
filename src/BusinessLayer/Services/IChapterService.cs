namespace BusinessLayer.Services;

public interface IChapterService
{
    Task<IReadOnlyList<ChapterDto>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken);
    Task<ChapterFormDto?> GetEditableAsync(Guid id, CancellationToken cancellationToken);
    Task<(Guid Id, Guid CourseId)> CreateAsync(Guid courseId, int order, string? clo, string title, string? summary, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, Guid courseId, int order, string? clo, string title, string? summary, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
