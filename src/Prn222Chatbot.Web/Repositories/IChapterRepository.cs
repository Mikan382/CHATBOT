using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Data;
using Prn222Chatbot.Web.Domain;

namespace Prn222Chatbot.Web.Repositories;

public interface IChapterRepository
{
    Task<IReadOnlyList<Chapter>> ListOrderedAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid chapterId, CancellationToken cancellationToken);
}

public class ChapterRepository : IChapterRepository
{
    private readonly AppDbContext _db;

    public ChapterRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Chapter>> ListOrderedAsync(CancellationToken cancellationToken)
    {
        return await _db.Chapters
            .OrderBy(x => x.Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid chapterId, CancellationToken cancellationToken)
    {
        return await _db.Chapters.AnyAsync(x => x.Id == chapterId, cancellationToken);
    }
}
