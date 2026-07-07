namespace BusinessLayer.Services;

public interface IDocumentService
{
    Task<(IReadOnlyList<ChapterSelectDto> Chapters, IReadOnlyList<DocumentIndexDto> Documents)> GetIndexDataAsync(
        string? searchTerm, Guid? courseId, Guid? chapterId, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseDto>> ListCoursesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentApiDto>> ListDocumentsAsync(CancellationToken cancellationToken);
    Task<DocumentDetailsDto> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunkApiDto>> ListChunksAsync(Guid documentId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> UploadAsync(Guid chapterId, Guid userId, Stream stream, string fileName, long fileSize, CancellationToken cancellationToken);
}

