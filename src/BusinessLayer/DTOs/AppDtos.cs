namespace BusinessLayer.Services;

public record CitationDto(Guid ChunkId, string SourceName, string ChapterTitle, int ChunkIndex, string Text);

public record SessionListDto(Guid Id, string Title, DateTime UpdatedAtUtc);

public record RetrievedChunkDto(Guid ChunkId, Guid DocumentId, string SourceName, string ChapterTitle, int ChunkIndex, string Content, double Score);

public record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    IReadOnlyList<CitationDto> Citations,
    string? Error,
    DateTime CreatedAtUtc,
    double? ProcessingSeconds = null);

public record ChatResponseDto(ChatMessageDto UserMessage, ChatMessageDto BotMessage);

public record ChatHistoryMessage(string Role, string Content);

public record ChapterDto(Guid Id, int Order, string Clo, string Title, string Summary);

public record CourseDto(Guid Id, string Code, string Name, string Description, string Tools, IReadOnlyList<ChapterDto> Chapters);

public record CourseListDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Tools,
    int ChaptersCount,
    IReadOnlyList<string> TeacherNames);

public record TeacherOptionDto(Guid Id, string Email, string DisplayName);

public record UserListDto(Guid Id, string Email, string DisplayName, string Role, bool IsLockedOut);

public record DocumentApiDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    DateTime UploadedAtUtc,
    ChapterDto? Chapter,
    int ChunksCount);

public record DocumentChunkApiDto(Guid Id, Guid DocumentId, int ChunkIndex, string SourceName, string Content, DateTime CreatedAtUtc);

public record DocumentIndexDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    DateTime UploadedAtUtc,
    string? CourseCode,
    string? ChapterTitle,
    int ChunksCount);

public record DocumentChunkViewDto(int ChunkIndex, string Content, IReadOnlyList<string> EmbeddingModels);

public record DocumentDetailsDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    DateTime UploadedAtUtc,
    string? CourseCode,
    string? CourseName,
    string? ChapterTitle,
    string? UploadedByEmail,
    string ContentText,
    string ContentHash,
    IReadOnlyList<DocumentChunkViewDto> Chunks);

public record ChapterSelectDto(Guid Id, Guid CourseId, int Order, string Title);

public record CourseFormDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Tools,
    IReadOnlyList<Guid> TeacherIds);

public record ChapterFormDto(Guid Id, Guid CourseId, int Order, string Clo, string Title, string Summary);

public record ChatSessionDto(Guid Id, Guid? CourseId);

public record ChunkingSettingsDto(string CurrentStrategy, IReadOnlyList<string> AvailableStrategies);
