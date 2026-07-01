using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IChapterRepository
{
    Task<IReadOnlyList<Chapter>> ListOrderedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Chapter>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken);
    Task<Chapter?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid chapterId, CancellationToken cancellationToken);
    Task AddAsync(Chapter chapter, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> OrderExistsAsync(Guid courseId, int order, Guid? excludeId, CancellationToken cancellationToken);
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
            .Include(x => x.Course)
            .OrderBy(x => x.Course!.Code)
            .ThenBy(x => x.Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Chapter>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken)
    {
        return await _db.Chapters
            .Include(x => x.Course)
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Chapter?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Chapters
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid chapterId, CancellationToken cancellationToken)
    {
        return await _db.Chapters.AnyAsync(x => x.Id == chapterId, cancellationToken);
    }

    public async Task AddAsync(Chapter chapter, CancellationToken cancellationToken)
    {
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var chapter = await _db.Chapters
            .Include(x => x.Documents)
            .Include(x => x.EvaluationQuestions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (chapter is null)
        {
            return false;
        }

        if (chapter.Documents.Count > 0 || chapter.EvaluationQuestions.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete a chapter that still has documents or evaluation questions.");
        }

        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> OrderExistsAsync(Guid courseId, int order, Guid? excludeId, CancellationToken cancellationToken)
    {
        return await _db.Chapters.AnyAsync(
            x => x.CourseId == courseId && x.Order == order && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);
    }
}
