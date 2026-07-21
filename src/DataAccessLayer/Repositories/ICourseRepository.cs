using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> ListAsync(string? searchTerm, Guid? teacherId, CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Course course, CancellationToken cancellationToken);
    Task SaveTeacherAssignmentAsync(Course course, Guid? teacherId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> HasChaptersAsync(Guid courseId, CancellationToken cancellationToken);
    Task<bool> TeacherCanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken cancellationToken);
    Task<bool> TeacherIsHeadOfCourseAsync(Guid courseId, Guid teacherId, CancellationToken cancellationToken);
}
