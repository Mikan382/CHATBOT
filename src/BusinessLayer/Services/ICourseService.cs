using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface ICourseService
{
    Task<IReadOnlyList<CourseDto>> ListDtosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseDto>> ListManageDtosAsync(Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseListDto>> ListManageAsync(string? searchTerm, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeacherOptionDto>> ListTeacherOptionsAsync(CancellationToken cancellationToken);
    Task<CourseFormDto?> GetEditableAsync(Guid id, CancellationToken cancellationToken);
    Task<CourseDto?> GetDetailsAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(string code, string name, string? description, string? tools, IReadOnlyList<Guid> teacherIds, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, string code, string name, string? description, string? tools, IReadOnlyList<Guid> teacherIds, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
