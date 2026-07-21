using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _db;

    public CourseRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Course>> ListAsync(string? searchTerm, Guid? teacherId, CancellationToken cancellationToken)
    {
        var query = _db.Courses
            .Include(x => x.Chapters)
            .Include(x => x.TeacherAssignments)
            .ThenInclude(x => x.Teacher)
            .AsSplitQuery()
            .AsQueryable();

        if (teacherId.HasValue)
        {
            query = query.Where(x => x.TeacherAssignments.Any(t => t.TeacherUserId == teacherId.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
        }

        return await query
            .OrderBy(x => x.Code)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Courses
            .Include(x => x.Chapters)
            .Include(x => x.TeacherAssignments)
            .ThenInclude(x => x.Teacher)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken)
    {
        _db.Courses.Add(course);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveTeacherAssignmentAsync(
        Course course,
        Guid? teacherId,
        CancellationToken cancellationToken)
    {
        // One teacher per course: drop every row that is not the selected teacher, then
        // add the selection if it is not already there. Keeping a matching existing row
        // preserves its AssignedAtUtc and avoids a delete+insert on the same primary key.
        var removals = course.TeacherAssignments
            .Where(x => x.TeacherUserId != teacherId)
            .ToList();

        foreach (var assignment in removals)
        {
            course.TeacherAssignments.Remove(assignment);
            _db.CourseTeachers.Remove(assignment);
        }

        if (teacherId.HasValue && course.TeacherAssignments.All(x => x.TeacherUserId != teacherId.Value))
        {
            course.TeacherAssignments.Add(new CourseTeacher
            {
                CourseId = course.Id,
                TeacherUserId = teacherId.Value,
                IsHead = true,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await _db.Courses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (course is null)
        {
            return false;
        }

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim();
        return await _db.Courses.AnyAsync(
            x => x.Code == normalized && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task<bool> HasChaptersAsync(Guid courseId, CancellationToken cancellationToken)
    {
        return await _db.Chapters.AnyAsync(x => x.CourseId == courseId, cancellationToken);
    }

    public async Task<bool> TeacherCanManageCourseAsync(Guid courseId, Guid teacherId, CancellationToken cancellationToken)
    {
        return await _db.CourseTeachers.AnyAsync(
            x => x.CourseId == courseId && x.TeacherUserId == teacherId,
            cancellationToken);
    }

    public async Task<bool> TeacherIsHeadOfCourseAsync(Guid courseId, Guid teacherId, CancellationToken cancellationToken)
    {
        return await _db.CourseTeachers.AnyAsync(
            x => x.CourseId == courseId && x.TeacherUserId == teacherId && x.IsHead,
            cancellationToken);
    }
}
