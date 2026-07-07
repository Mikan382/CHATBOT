using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ICourseRepository
{
    Task<Course> GetCurrentWithChaptersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Course>> ListAsync(string? searchTerm, Guid? teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Course>> ListWithChaptersAsync(Guid? teacherId, CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Course?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Course course, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> HasChaptersAsync(Guid courseId, CancellationToken cancellationToken);
    Task<bool> TeacherCanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken cancellationToken);
    Task SetTeacherAssignmentsAsync(Guid courseId, IReadOnlyList<Guid> teacherIds, CancellationToken cancellationToken);
}
