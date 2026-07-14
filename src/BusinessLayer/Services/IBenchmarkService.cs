using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IBenchmarkService
{
    Task<BenchmarkDashboardDto> GetDashboardAsync(Guid? courseId, CancellationToken cancellationToken);

    Task<BenchmarkQuestionPageDto> ListQuestionsAsync(
        Guid? courseId,
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<BenchmarkQuestionEditorDto> GetCreateQuestionAsync(Guid? courseId, CancellationToken cancellationToken);
    Task<BenchmarkQuestionEditorDto?> GetEditQuestionAsync(Guid id, CancellationToken cancellationToken);

    Task CreateQuestionAsync(
        Guid courseId,
        Guid expectedDocumentId,
        string question,
        string expectedAnswer,
        int displayOrder,
        bool isActive,
        CancellationToken cancellationToken);

    Task UpdateQuestionAsync(
        Guid id,
        Guid courseId,
        Guid expectedDocumentId,
        string question,
        string expectedAnswer,
        int displayOrder,
        bool isActive,
        CancellationToken cancellationToken);

    Task DeleteQuestionAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> RunAsync(Guid courseId, string chunkingStrategy, string embeddingModel, int topK, CancellationToken cancellationToken);
    Task<BenchmarkRunDetailsDto?> GetRunAsync(Guid id, CancellationToken cancellationToken);
}
