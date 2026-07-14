namespace BusinessLayer.DTOs;

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

public record ChatHistoryMessage(string Role, string Content);

public record ChatQuotaStatusDto(string PlanName, int Quota, int Used, bool Unlimited)
{
    public bool Exhausted => !Unlimited && Used >= Quota;
    public int Remaining => Unlimited ? 0 : Math.Max(0, Quota - Used);
}

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

public record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string Role,
    DateTime UpdatedAtUtc);

public record DocumentIndexDto(
    Guid Id,
    string OriginalFileName,
    string FileType,
    long FileSizeBytes,
    DateTime UploadedAtUtc,
    string? CourseCode,
    string? ChapterTitle,
    int ChunksCount,
    string ChunkingStrategy);

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
    string ChunkingStrategy,
    IReadOnlyList<DocumentChunkViewDto> Chunks);

public record ChapterSelectDto(Guid Id, Guid CourseId, int Order, string Title);

public record DocumentIndexPageDto(
    IReadOnlyList<CourseDto> Courses,
    IReadOnlyList<ChapterSelectDto> Chapters,
    IReadOnlyList<DocumentIndexDto> Documents);

public record CourseFormDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Tools,
    IReadOnlyList<Guid> TeacherIds);

public record ChapterFormDto(Guid Id, Guid CourseId, int Order, string Clo, string Title, string Summary);

public record ChatSessionDto(Guid Id, Guid? CourseId);

public record ChunkingSettingsDto(
    string CurrentStrategy,
    int FixedChunkSize,
    int FixedChunkOverlap,
    IReadOnlyList<string> AvailableStrategies);

public record SubscriptionPlanDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal MonthlyPrice,
    int DurationDays,
    int MessageQuota,
    int SortOrder,
    bool IsActive);

public record StudentSubscriptionDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    string PlanCode,
    string Status,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc);

public record SubscriptionRequestDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    string PlanCode,
    DateTime RequestedAtUtc);

public record StudentSubscriptionPageDto(
    StudentSubscriptionDto? CurrentSubscription,
    SubscriptionRequestDto? PendingRequest,
    IReadOnlyList<SubscriptionPlanDto> AvailablePlans);

public record SubscriptionPlanStatsDto(
    Guid PlanId,
    string PlanName,
    string PlanCode,
    bool IsActive,
    int ActiveSubscriptions,
    decimal EstimatedMonthlyRevenue);

public record RecentSubscriptionDto(
    Guid Id,
    string StudentEmail,
    string StudentDisplayName,
    string PlanName,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? ExpiresAtUtc);

public record SubscriptionDashboardDto(
    int TotalStudents,
    int ActiveSubscriptions,
    int RequestsThisMonth,
    decimal EstimatedMonthlyRevenue,
    IReadOnlyList<RecentSubscriptionDto> PendingSubscriptions,
    IReadOnlyList<SubscriptionPlanStatsDto> PlanStats,
    IReadOnlyList<RecentSubscriptionDto> RecentSubscriptions,
    IReadOnlyList<SubscriptionPlanDto> Plans);
