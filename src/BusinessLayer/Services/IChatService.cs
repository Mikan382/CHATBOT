using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IChatService
{
    bool GeminiConfigured { get; }
    Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<ChatMessageDto> SaveUserMessageAsync(Guid sessionId, Guid userId, Guid courseId, string text, CancellationToken cancellationToken);
    Task<ChatMessageDto> GenerateAssistantReplyAsync(Guid sessionId, Guid userId, Guid courseId, string text, CancellationToken cancellationToken);
    Task ClearAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionListDto>> ListSessionsAsync(Guid userId, string? searchTerm, CancellationToken cancellationToken);
    Task<string?> RenameSessionAsync(Guid sessionId, Guid userId, string title, CancellationToken cancellationToken);
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}
