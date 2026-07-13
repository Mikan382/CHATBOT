using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IDocumentService
{
    Task<DocumentIndexPageDto> GetIndexDataAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        Guid userId,
        bool isAdmin,
        bool isTeacher,
        CancellationToken cancellationToken);

    Task<DocumentDetailsDto> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<Guid> UploadAsync(Guid chapterId, Guid userId, bool isAdmin, Stream stream, string fileName, long fileSize, CancellationToken cancellationToken);
}
