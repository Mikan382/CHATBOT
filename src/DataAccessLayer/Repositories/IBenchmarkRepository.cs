using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IBenchmarkRepository
{
    Task<IReadOnlyList<EvaluationQuestion>> ListQuestionsAsync(
        Guid? courseId,
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluationQuestion>> ListActiveQuestionsAsync(Guid courseId, CancellationToken cancellationToken);
    Task<EvaluationQuestion?> GetQuestionAsync(Guid id, CancellationToken cancellationToken);
    Task AddQuestionAsync(EvaluationQuestion question, CancellationToken cancellationToken);
    Task SaveQuestionAsync(CancellationToken cancellationToken);
    Task<bool> DeleteQuestionAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> ListDocumentsAsync(Guid? courseId, CancellationToken cancellationToken);
    Task AddRunAsync(BenchmarkRun run, CancellationToken cancellationToken);
    Task<IReadOnlyList<BenchmarkRun>> ListRunsAsync(Guid? courseId, int take, CancellationToken cancellationToken);
    Task<BenchmarkRun?> GetRunAsync(Guid id, CancellationToken cancellationToken);
}
