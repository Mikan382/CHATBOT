using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Data;
using Prn222Chatbot.Web.Domain;

namespace Prn222Chatbot.Web.Repositories;

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
