namespace BusinessLayer.Services;

public enum ChatModelType
{
    RagStandard = 1,
    FineTunedOnly = 2,
    RagHybrid = 3
}

public record CitationDto(Guid ChunkId, string SourceName, string ChapterTitle, int ChunkIndex, string Text);

public record SessionListDto(Guid Id, string Title, DateTime UpdatedAtUtc);

public record RetrievedChunkDto(Guid ChunkId, Guid DocumentId, string SourceName, string ChapterTitle, int ChunkIndex, string Content, double Score);

public record ChatMessageDto(Guid Id, string Role, string ModelType, string Content, IReadOnlyList<CitationDto> Citations, string? Error, DateTime CreatedAtUtc, double? ProcessingSeconds = null);

public record ChatResponseDto(ChatMessageDto UserMessage, ChatMessageDto BotMessage);

public record FineTuneHistoryMessage(string Role, string Content);

public record FineTuneRequest(string SessionId, string CourseCode, string Question, IReadOnlyList<FineTuneHistoryMessage> History);

public record FineTuneResponse(string Answer, string? ModelName, int? LatencyMs);

public record EvaluationScore(decimal Faithfulness, decimal AnswerRelevance, decimal RetrievalRecall, decimal CitationAccuracy);

public record ChapterDto(Guid Id, int Order, string Clo, string Title, string Summary);

public record CourseDto(Guid Id, string Code, string Name, string Description, string Tools, IReadOnlyList<ChapterDto> Chapters);

public record CourseListDto(Guid Id, string Code, string Name, string Description, string Tools, int ChaptersCount);

public record UserListDto(Guid Id, string Email, string FullName, string Role, bool IsLockedOut);

public record DocumentApiDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    string IndexStatus,
    int IndexProgressPercent,
    string IndexStage,
    string? IndexError,
    DateTime UploadedAtUtc,
    ChapterDto? Chapter,
    int ChunksCount);

public record DocumentChunkApiDto(Guid Id, Guid DocumentId, int ChunkIndex, string SourceName, string Content, DateTime CreatedAtUtc);

// --- DTOs for MVC views (replacing raw entity returns) ---

/// <summary>Document list item for Documents/Index view.</summary>
public record DocumentIndexDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    string IndexStatus,
    int IndexProgressPercent,
    string IndexStage,
    string? IndexError,
    DateTime UploadedAtUtc,
    string? CourseCode,
    string? ChapterTitle,
    int ChunksCount);

/// <summary>Chunk view item for Documents/Details view.</summary>
public record DocumentChunkViewDto(int ChunkIndex, string Content, IReadOnlyList<string> EmbeddingModels);

/// <summary>Document details for Documents/Details view.</summary>
public record DocumentDetailsDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    string IndexStatus,
    int IndexProgressPercent,
    string IndexStage,
    string? IndexError,
    DateTime UploadedAtUtc,
    string? CourseCode,
    string? CourseName,
    string? ChapterTitle,
    string? UploadedByEmail,
    string ContentText,
    IReadOnlyList<DocumentChunkViewDto> Chunks);

/// <summary>Chapter item for dropdowns (includes CourseId for filtering).</summary>
public record ChapterSelectDto(Guid Id, Guid CourseId, int Order, string Title);

/// <summary>Course form data for Edit view.</summary>
public record CourseFormDto(Guid Id, string Code, string Name, string Description, string Tools);

/// <summary>Chapter form data for Edit view.</summary>
public record ChapterFormDto(Guid Id, Guid CourseId, int Order, string Clo, string Title, string Summary);

/// <summary>Chat session info (only CourseId needed by PL).</summary>
public record ChatSessionDto(Guid Id, Guid? CourseId);

/// <summary>Evaluation question for Benchmark view.</summary>
public record EvaluationQuestionDto(Guid Id, string Question, string GroundTruth);

/// <summary>Evaluation result for Benchmark view.</summary>
public record EvaluationResultViewDto(
    Guid Id,
    string? Question,
    string? GroundTruth,
    string Status,
    decimal Faithfulness,
    decimal AnswerRelevance,
    decimal RetrievalRecall,
    decimal CitationAccuracy,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    string ChunkingStrategy,
    string EmbeddingModelName,
    int RagLatencyMs,
    int FineTunedLatencyMs,
    string RagAnswer,
    string? FineTunedAnswer,
    decimal FtFaithfulness,
    decimal FtAnswerRelevance);

/// <summary>Aggregated benchmark row (by model or by strategy).</summary>
public record BenchmarkAggregateRow(string Label, decimal AvgFaithfulness, decimal AvgRelevance, decimal AvgRecall, decimal AvgCitation, int Count);

/// <summary>Full dashboard data for Benchmark view (aggregation done in BL, not in view).</summary>
public record BenchmarkDashboardDto(
    IReadOnlyList<EvaluationQuestionDto> Questions,
    IReadOnlyList<EvaluationResultViewDto> Results,
    IReadOnlyList<BenchmarkAggregateRow> ByModel,
    IReadOnlyList<BenchmarkAggregateRow> ByStrategy,
    IReadOnlyList<EvaluationResultViewDto> RagVsFt);

public record EvaluationResultApiDto(
    Guid Id,
    string Question,
    string Status,
    decimal Faithfulness,
    decimal AnswerRelevance,
    decimal RetrievalRecall,
    decimal CitationAccuracy,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    string ChunkingStrategy,
    string EmbeddingModelName,
    int RagLatencyMs,
    int FineTunedLatencyMs,
    string RagAnswer,
    string? FineTunedAnswer,
    decimal FtFaithfulness,
    decimal FtAnswerRelevance);

public static class ModelTypeParser
{
    public static bool TryParse(string? input, out ChatModelType modelType)
    {
        modelType = input switch
        {
            "rag_standard" => ChatModelType.RagStandard,
            "fine_tuned_only" => ChatModelType.FineTunedOnly,
            "rag_hybrid" => ChatModelType.RagHybrid,
            _ => default
        };

        return modelType != default;
    }

    public static string ToClientValue(this ChatModelType modelType)
    {
        return modelType switch
        {
            ChatModelType.RagStandard => "rag_standard",
            ChatModelType.FineTunedOnly => "fine_tuned_only",
            ChatModelType.RagHybrid => "rag_hybrid",
            _ => "rag_standard"
        };
    }
}
