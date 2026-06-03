using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ICourseRepository
{
    Task<Course> GetCurrentWithChaptersAsync(CancellationToken cancellationToken);
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
            .FirstAsync(cancellationToken);
    }
}
