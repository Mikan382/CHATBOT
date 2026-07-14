using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class BenchmarkRepository : IBenchmarkRepository
{
    private readonly AppDbContext _db;

    public BenchmarkRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EvaluationQuestion>> ListQuestionsAsync(
        Guid? courseId,
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _db.EvaluationQuestions
            .Include(x => x.Course)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(x => x.CourseId == courseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x => x.Question.Contains(term) || x.ExpectedAnswer.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.Course!.Code)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Question)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvaluationQuestion>> ListActiveQuestionsAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        return await _db.EvaluationQuestions
            .Where(x => x.CourseId == courseId && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Question)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<EvaluationQuestion?> GetQuestionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.EvaluationQuestions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddQuestionAsync(EvaluationQuestion question, CancellationToken cancellationToken)
    {
        _db.EvaluationQuestions.Add(question);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveQuestionAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteQuestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await _db.EvaluationQuestions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (question is null)
        {
            return false;
        }

        _db.EvaluationQuestions.Remove(question);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Document>> ListDocumentsAsync(Guid? courseId, CancellationToken cancellationToken)
    {
        var query = _db.Documents
            .Include(x => x.Chapter)
            .ThenInclude(x => x!.Course)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(x => x.Chapter!.CourseId == courseId.Value);
        }

        return await query
            .OrderBy(x => x.Chapter!.Course!.Code)
            .ThenBy(x => x.Chapter!.Order)
            .ThenBy(x => x.OriginalFileName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddRunAsync(BenchmarkRun run, CancellationToken cancellationToken)
    {
        _db.BenchmarkRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BenchmarkRun>> ListRunsAsync(
        Guid? courseId,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _db.BenchmarkRuns
            .Include(x => x.Results)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(x => x.CourseId == courseId.Value);
        }

        return await query
            .OrderByDescending(x => x.CompletedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BenchmarkRun?> GetRunAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.BenchmarkRuns
            .Include(x => x.Results.OrderBy(result => result.DisplayOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
