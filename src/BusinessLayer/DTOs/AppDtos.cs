using DataAccessLayer.Enums;

namespace BusinessLayer.Services;

public record CitationDto(Guid ChunkId, string SourceName, string ChapterTitle, int ChunkIndex, string Text);

public record RetrievedChunkDto(Guid ChunkId, Guid DocumentId, string SourceName, string ChapterTitle, int ChunkIndex, string Content, double Score);

public record ChatMessageDto(Guid Id, string Role, string ModelType, string Content, IReadOnlyList<CitationDto> Citations, string? Error, DateTime CreatedAtUtc);

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

public record EvaluationResultApiDto(
    Guid Id,
    string Question,
    string Status,
    decimal Faithfulness,
    decimal AnswerRelevance,
    decimal RetrievalRecall,
    decimal CitationAccuracy,
    string? ErrorMessage,
    DateTime CreatedAtUtc);

public static class ModelTypeParser
{
    public static bool TryParse(string? input, out ModelType modelType)
    {
        modelType = input switch
        {
            "rag_standard" => ModelType.RagStandard,
            "fine_tuned_only" => ModelType.FineTunedOnly,
            "rag_hybrid" => ModelType.RagHybrid,
            _ => default
        };

        return modelType != default;
    }

    public static string ToClientValue(this ModelType modelType)
    {
        return modelType switch
        {
            ModelType.RagStandard => "rag_standard",
            ModelType.FineTunedOnly => "fine_tuned_only",
            ModelType.RagHybrid => "rag_hybrid",
            _ => "rag_standard"
        };
    }
}
