using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IChapterService
{
    Task<ChapterFormDto?> GetEditableAsync(Guid id, Guid userId, string userRole, CancellationToken cancellationToken);
    Task<(Guid Id, Guid CourseId)> CreateAsync(Guid courseId, int order, string? clo, string title, string? summary, Guid userId, string userRole, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, Guid courseId, int order, string? clo, string title, string? summary, Guid userId, string userRole, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid userId, string userRole, CancellationToken cancellationToken);
}
