using DataAccessLayer.Enums;

namespace BusinessLayer.Services;

public interface IChatService
{
    bool GeminiConfigured { get; }
    bool FineTuneConfigured { get; }
    Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<ChatMessageDto> SaveUserMessageAsync(Guid sessionId, Guid userId, Guid courseId, ModelType modelType, string text, CancellationToken cancellationToken);
    Task<ChatMessageDto> GenerateAssistantReplyAsync(Guid sessionId, Guid userId, Guid courseId, ModelType modelType, string text, CancellationToken cancellationToken);
    Task<ChatResponseDto> SendAsync(Guid sessionId, Guid userId, Guid courseId, ModelType modelType, string text, CancellationToken cancellationToken);
    Task ClearAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionListDto>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}
