using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ICourseRepository
{
    Task<Course> GetCurrentWithChaptersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Course>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<IReadOnlyList<Course>> ListWithChaptersAsync(CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Course?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Course course, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> HasChaptersAsync(Guid courseId, CancellationToken cancellationToken);
}

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _db;

    public CourseRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Course> GetCurrentWithChaptersAsync(CancellationToken cancellationToken)
    {
        return await _db.Courses
            .Include(x => x.Chapters)
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = _db.Courses
            .Include(x => x.Chapters)
            .AsQueryable();

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

    public async Task<IReadOnlyList<Course>> ListWithChaptersAsync(CancellationToken cancellationToken)
    {
        return await _db.Courses
            .Include(x => x.Chapters)
            .OrderBy(x => x.Code)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Courses
            .Include(x => x.Chapters)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Course?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.Courses
            .Include(x => x.Chapters)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken)
    {
        _db.Courses.Add(course);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
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
}
