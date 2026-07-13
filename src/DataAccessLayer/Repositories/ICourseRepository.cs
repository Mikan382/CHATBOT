using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> ListAsync(string? searchTerm, Guid? teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Course>> ListWithChaptersAsync(Guid? teacherId, CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Course course, CancellationToken cancellationToken);
    Task SaveWithTeacherAssignmentsAsync(Course course, IReadOnlyList<Guid> teacherIds, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> HasChaptersAsync(Guid courseId, CancellationToken cancellationToken);
    Task<bool> TeacherCanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken cancellationToken);
}
