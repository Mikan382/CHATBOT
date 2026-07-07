namespace BusinessLayer.Services;

public interface ICourseService
{
    Task<CourseDto> GetCurrentAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseDto>> ListDtosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseListDto>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<CourseFormDto?> GetEditableAsync(Guid id, CancellationToken cancellationToken);
    Task<CourseDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(string code, string name, string? description, string? tools, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, string code, string name, string? description, string? tools, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChapterDto>> ListChaptersAsync(Guid courseId, CancellationToken cancellationToken);
}
