using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IEvaluationRepository
{
    Task<IReadOnlyList<EvaluationQuestion>> ListQuestionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EvaluationQuestion>> ListQuestionsForRunAsync(int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvaluationResult>> ListRecentResultsAsync(int take, CancellationToken cancellationToken);
    Task SaveResultsAsync(IReadOnlyList<EvaluationResult> results, CancellationToken cancellationToken);
}

public class EvaluationRepository : IEvaluationRepository
{
    private readonly AppDbContext _db;

    public EvaluationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EvaluationQuestion>> ListQuestionsAsync(CancellationToken cancellationToken)
    {
        return await _db.EvaluationQuestions
            .Include(x => x.Chapter)
            .OrderBy(x => x.Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvaluationQuestion>> ListQuestionsForRunAsync(int limit, CancellationToken cancellationToken)
    {
        return await _db.EvaluationQuestions
            .Include(x => x.Chapter)
            .ThenInclude(x => x!.Course)
            .OrderBy(x => x.Order)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvaluationResult>> ListRecentResultsAsync(int take, CancellationToken cancellationToken)
    {
        return await _db.EvaluationResults
            .Include(x => x.EvaluationQuestion)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveResultsAsync(IReadOnlyList<EvaluationResult> results, CancellationToken cancellationToken)
    {
        _db.EvaluationResults.AddRange(results);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
