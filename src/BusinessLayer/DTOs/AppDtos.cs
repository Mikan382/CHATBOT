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

public record ChatQuotaStatusDto(
    string PlanName,
    long Quota,
    long Used,
    DateTime? ExpiresAtUtc,
    bool HasActiveSubscription)
{
    public bool Exhausted => !HasActiveSubscription || Used >= Quota;
    public long Remaining => Math.Max(0, Quota - Used);
}

public record AcceptedChatMessageDto(
    ChatMessageDto Message,
    ChatQuotaStatusDto? Quota,
    Guid? StudentSubscriptionId);

public record ChapterDto(Guid Id, int Order, string Clo, string Title, string Summary);

public record CourseDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Tools,
    IReadOnlyList<ChapterDto> Chapters,
    string? DefaultChunkingStrategy = null,
    int? DefaultChunkSize = null,
    int? DefaultChunkOverlap = null,
    string? DefaultEmbeddingModel = null);

public record CourseListDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Tools,
    int ChaptersCount,
    string? TeacherName,
    string? DefaultChunkingStrategy = null,
    int? DefaultChunkSize = null,
    int? DefaultChunkOverlap = null,
    string? DefaultEmbeddingModel = null);

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
    Guid? CourseId,
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
    IReadOnlyList<DocumentIndexDto> Documents,
    Guid? SelectedCourseId,
    Guid? SelectedChapterId,
    // Courses the caller is head teacher of, i.e. the only ones they may upload to or delete from.
    IReadOnlyList<Guid> ManageableCourseIds);

public record CourseFormDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Tools,
    IReadOnlyList<Guid> TeacherIds,
    Guid? HeadTeacherId,
    string? DefaultChunkingStrategy = null,
    int? DefaultChunkSize = null,
    int? DefaultChunkOverlap = null,
    string? DefaultEmbeddingModel = null);

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
    decimal Price,
    int DurationDays,
    long TokenQuota,
    int SortOrder,
    bool IsActive,
    bool IsDefault);

public record StudentSubscriptionDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    string PlanCode,
    string Status,
    bool IsFreePackage,
    decimal PriceAtActivation,
    long TokenQuota,
    long InputTokensUsed,
    long OutputTokensUsed,
    long TotalTokensUsed,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc);

public record StudentSubscriptionPageDto(
    StudentSubscriptionDto? CurrentSubscription,
    IReadOnlyList<SubscriptionPlanDto> AvailablePlans,
    bool PaymentConfigured);

public record SubscriptionPlanStatsDto(
    Guid PlanId,
    string PlanName,
    string PlanCode,
    bool IsActive,
    bool IsDefault,
    int ActiveSubscriptions,
    decimal ActivePackageValue);

public record RecentSubscriptionDto(
    Guid Id,
    string StudentEmail,
    string StudentDisplayName,
    string PlanName,
    string Status,
    DateTime ActivatedAtUtc,
    DateTime? ExpiresAtUtc);

public record RecentPaymentDto(
    Guid Id,
    string StudentEmail,
    string StudentDisplayName,
    string PlanName,
    decimal Amount,
    string Status,
    DateTime OccurredAtUtc);

public record SubscriptionDashboardDto(
    int TotalStudents,
    int ActiveSubscriptions,
    int PaidActivations,
    decimal Revenue,
    long TotalTokensUsed,
    long ActiveTokenQuota,
    decimal TokenUtilization,
    int NeedsAttentionCount,
    IReadOnlyList<SubscriptionTrendPointDto> Trend,
    IReadOnlyList<PackagePerformanceDto> PackagePerformance,
    IReadOnlyList<ExpiringSubscriptionDto> ExpiringSubscriptions,
    int ExpiringSubscriptionsTotal,
    IReadOnlyList<FailedPaymentDto> FailedPayments,
    int FailedPaymentsTotal,
    IReadOnlyList<UserTokenUsageDto> UsersNearTokenLimit,
    IReadOnlyList<SubscriptionPlanStatsDto> PlanStats,
    IReadOnlyList<RecentSubscriptionDto> RecentSubscriptions,
    IReadOnlyList<RecentPaymentDto> RecentPayments,
    IReadOnlyList<SubscriptionPlanDto> Plans);

public record SubscriptionTrendPointDto(DateOnly Date, decimal Revenue, int PaidActivations);

public record PackagePerformanceDto(
    Guid PackageId,
    string PackageCode,
    string PackageName,
    int ActiveUsers,
    int PaidActivations,
    decimal Revenue,
    long TokenUsed,
    long TokenQuota,
    decimal? Utilization);

public record ExpiringSubscriptionDto(
    Guid SubscriptionId,
    string StudentName,
    string StudentEmail,
    string PackageName,
    DateTime ExpiresAtUtc,
    int DaysLeft);

public record FailedPaymentDto(
    Guid PaymentId,
    string StudentName,
    string StudentEmail,
    string PackageName,
    decimal Amount,
    DateTime FailedAtUtc);

public record UserTokenUsageDto(
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string PackageName,
    long TokensUsed,
    long TokenQuota,
    long TokensRemaining,
    decimal Utilization);

public record ActiveSubscriptionDetailDto(
    Guid SubscriptionId,
    string StudentName,
    string StudentEmail,
    string PackageCode,
    string PackageName,
    long TokensUsed,
    long TokenQuota,
    long TokensRemaining,
    decimal Utilization,
    DateTime ActivatedAtUtc,
    DateTime? ExpiresAtUtc);

