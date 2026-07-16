using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IChatRepository
{
    Task<ChatSession> EnsureSessionAsync(Guid sessionId, Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task<ChatSession?> GetOwnedSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> ListMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken);
    Task UpdateSessionTitleFromFirstUserMessageAsync(Guid sessionId, Guid userId, string messageText, CancellationToken cancellationToken);
    Task AddAssistantMessageAndTouchSessionAsync(
        ChatMessage message,
        Guid sessionId,
        Guid userId,
        ChatTokenUsageUpdate? tokenUsage,
        CancellationToken cancellationToken);
    Task ClearMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> ListRecentMessagesAsync(Guid sessionId, Guid userId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatSession>> ListSessionsAsync(Guid userId, string? searchTerm, int take, CancellationToken cancellationToken);
    Task<bool> RenameSessionAsync(Guid sessionId, Guid userId, string title, CancellationToken cancellationToken);
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}

public record ChatTokenUsageUpdate(
    Guid StudentSubscriptionId,
    long InputTokens,
    long OutputTokens,
    long TotalTokens);
