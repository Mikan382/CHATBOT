namespace BusinessLayer.DTOs;

public record BenchmarkCourseOptionDto(Guid Id, string Code, string Name);

public record BenchmarkDocumentOptionDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string ChapterTitle,
    string FileName);

public record BenchmarkQuestionDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    Guid ExpectedDocumentId,
    string ExpectedSourceName,
    string Question,
    string ExpectedAnswer,
    int DisplayOrder,
    bool IsActive);

public record BenchmarkQuestionPageDto(
    IReadOnlyList<BenchmarkCourseOptionDto> Courses,
    IReadOnlyList<BenchmarkQuestionDto> Questions,
    Guid? SelectedCourseId,
    string? SearchTerm,
    bool? IsActive);

public record BenchmarkQuestionEditorDto(
    BenchmarkQuestionDto? Question,
    IReadOnlyList<BenchmarkCourseOptionDto> Courses,
    IReadOnlyList<BenchmarkDocumentOptionDto> Documents,
    Guid? SelectedCourseId,
    int SuggestedDisplayOrder);

public record BenchmarkRunSummaryDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string ChunkingStrategy,
    string EmbeddingModelName,
    int TopK,
    int QuestionCount,
    int ChunkCount,
    double HitRate,
    double MeanReciprocalRank,
    double AverageAnswerTokenF1,
    double AverageLatencyMilliseconds,
    long DurationMilliseconds,
    DateTime CompletedAtUtc);

public record BenchmarkDashboardDto(
    IReadOnlyList<BenchmarkCourseOptionDto> Courses,
    Guid? SelectedCourseId,
    int TotalQuestions,
    int ActiveQuestions,
    IReadOnlyList<string> AvailableChunkingStrategies,
    IReadOnlyList<string> AvailableEmbeddingModels,
    bool GeminiConfigured,
    bool EmbeddingConfigured,
    IReadOnlyList<BenchmarkRunSummaryDto> LatestComparisons,
    IReadOnlyList<BenchmarkRunSummaryDto> RecentRuns);

public record BenchmarkRetrievedSourceDto(
    int Rank,
    Guid DocumentId,
    string SourceName,
    string ChapterTitle,
    int ChunkIndex,
    double Score);

public record BenchmarkResultDetailDto(
    Guid Id,
    int DisplayOrder,
    string Question,
    string ExpectedAnswer,
    string ExpectedSourceName,
    string GeneratedAnswer,
    IReadOnlyList<BenchmarkRetrievedSourceDto> RetrievedSources,
    bool HitAtK,
    double ReciprocalRank,
    double AnswerTokenF1,
    long LatencyMilliseconds);

public record BenchmarkRunDetailsDto(
    BenchmarkRunSummaryDto Run,
    string CourseName,
    IReadOnlyList<BenchmarkResultDetailDto> Results);
