using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BusinessLayer.AI;
using BusinessLayer.DTOs;
using BusinessLayer.Retrieval;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class ChatService : IChatService
{
    private const int MaxMessageLength = 4000;
    private const int MaxSessionSearchLength = 100;
    private readonly IChatRepository _chatRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly RetrievalService _retrievalService;
    private readonly IGeminiClient _geminiClient;
    private readonly RagOptions _ragOptions;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository chatRepository,
        ICourseRepository courseRepository,
        ISubscriptionRepository subscriptionRepository,
        RetrievalService retrievalService,
        IGeminiClient geminiClient,
        IOptions<RagOptions> ragOptions,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _courseRepository = courseRepository;
        _subscriptionRepository = subscriptionRepository;
        _retrievalService = retrievalService;
        _geminiClient = geminiClient;
        _ragOptions = ragOptions.Value;
        _logger = logger;
    }

    public bool AssistantConfigured => _geminiClient.IsConfigured && _retrievalService.IsConfigured;

    public async Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _chatRepository.GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session is null) return null;
        return new ChatSessionDto(session.Id, session.CourseId);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var messages = await _chatRepository.ListMessagesAsync(sessionId, userId, cancellationToken);
        return messages.Select(m => ToDto(m)).ToList();
    }

    public async Task<ChatQuotaStatusDto> GetQuotaStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var active = await GetActiveSubscriptionAsync(userId, cancellationToken);
        if (active?.Plan is null)
        {
            return new ChatQuotaStatusDto("No active package", 0, 0, false, false);
        }

        var quota = active.MessageQuotaAtActivation;
        var unlimited = quota == 0;
        var periodKey = PeriodKey(active.Id);
        var used = await _chatRepository.GetUsageAsync(userId, periodKey, cancellationToken);
        return new ChatQuotaStatusDto(active.Plan.Name, quota, used, unlimited, true);
    }

    public async Task<AcceptedChatMessageDto> AcceptUserMessageAsync(
        Guid sessionId,
        Guid userId,
        Guid courseId,
        string text,
        bool enforceQuota,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Message is empty.");
        }

        if (text.Length > MaxMessageLength)
        {
            throw new InvalidOperationException($"Message cannot exceed {MaxMessageLength} characters.");
        }

        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        await _chatRepository.EnsureSessionAsync(sessionId, userId, courseId, cancellationToken);

        var quota = enforceQuota
            ? await ConsumeMessageUsageAsync(userId, cancellationToken)
            : null;

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = ChatRole.User,
            Content = text.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _chatRepository.AddMessageAsync(userMessage, cancellationToken);
        await _chatRepository.UpdateSessionTitleFromFirstUserMessageAsync(sessionId, userId, text.Trim(), cancellationToken);
        return new AcceptedChatMessageDto(ToDto(userMessage), quota);
    }

    public async Task<ChatMessageDto> GenerateAssistantReplyAsync(Guid sessionId, Guid userId, Guid courseId, string text, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        var history = await BuildHistoryAsync(sessionId, userId, cancellationToken, excludeLatestUserMessage: true);
        var citations = new List<CitationDto>();
        string answer;
        string? error = null;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            answer = await GenerateRagAsync(
                course.Id,
                course.Name,
                text,
                history,
                citations,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Assistant generation failed for session {SessionId}.", sessionId);
            error = "Assistant request failed.";
            answer = "The assistant is temporarily unavailable. Please try again.";
        }
        finally
        {
            stopwatch.Stop();
        }

        var botMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = ChatRole.Assistant,
            Content = answer,
            CitationsJson = citations.Count > 0 ? JsonSerializer.Serialize(citations) : null,
            Error = error,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _chatRepository.AddAssistantMessageAndTouchSessionAsync(botMessage, sessionId, userId, cancellationToken);
        return ToDto(botMessage, stopwatch.Elapsed.TotalSeconds);
    }

    public async Task ClearAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await _chatRepository.ClearMessagesAsync(sessionId, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<SessionListDto>> ListSessionsAsync(
        Guid userId,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (normalizedSearch?.Length > MaxSessionSearchLength)
        {
            throw new InvalidOperationException($"Search cannot exceed {MaxSessionSearchLength} characters.");
        }

        var take = string.IsNullOrWhiteSpace(normalizedSearch) ? 20 : 50;
        var sessions = await _chatRepository.ListSessionsAsync(userId, normalizedSearch, take, cancellationToken);
        return sessions.Select(s => new SessionListDto(s.Id, s.Title, s.UpdatedAtUtc)).ToList();
    }

    public async Task<string?> RenameSessionAsync(Guid sessionId, Guid userId, string title, CancellationToken cancellationToken)
    {
        var normalizedTitle = NormalizeSessionTitle(title);
        var renamed = await _chatRepository.RenameSessionAsync(sessionId, userId, normalizedTitle, cancellationToken);
        return renamed ? normalizedTitle : null;
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        return await _chatRepository.DeleteSessionAsync(sessionId, userId, cancellationToken);
    }

    private async Task<string> GenerateRagAsync(
        Guid courseId,
        string courseName,
        string text,
        IReadOnlyList<ChatHistoryMessage> history,
        List<CitationDto> citations,
        CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveAsync(text, courseId, cancellationToken);
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return "The indexed documents do not contain enough relevant information to answer this question accurately.";
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        return await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(courseName), prompt, cancellationToken);
    }

    private async Task<IReadOnlyList<ChatHistoryMessage>> BuildHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken, bool excludeLatestUserMessage = false)
    {
        var messages = await _chatRepository.ListRecentMessagesAsync(
            sessionId,
            userId,
            _ragOptions.HistoryMessageCount,
            cancellationToken);
        if (excludeLatestUserMessage && messages.Count > 0 && messages[^1].Role == ChatRole.User)
        {
            messages = messages.Take(messages.Count - 1).ToList();
        }

        return messages
            .Select(x => new ChatHistoryMessage(x.Role == ChatRole.User ? "user" : "assistant", x.Content))
            .ToList();
    }

    private static CitationDto ToCitation(RetrievedChunkDto chunk)
    {
        return new CitationDto(chunk.ChunkId, chunk.SourceName, chunk.ChapterTitle, chunk.ChunkIndex, chunk.Content);
    }

    private async Task<ChatQuotaStatusDto> ConsumeMessageUsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var active = await GetActiveSubscriptionAsync(userId, cancellationToken);
        if (active?.Plan is null)
        {
            throw new InvalidOperationException("No active subscription package. Activate or purchase a package to continue.");
        }

        var quota = active.MessageQuotaAtActivation;
        var periodKey = PeriodKey(active.Id);
        var consumed = await _chatRepository.TryConsumeUsageAsync(userId, periodKey, quota, cancellationToken);
        if (!consumed && quota > 0)
        {
            throw new InvalidOperationException(
                $"You have used all {quota} questions in the {active.Plan.Name} package.");
        }

        var used = await _chatRepository.GetUsageAsync(userId, periodKey, cancellationToken);
        return new ChatQuotaStatusDto(active.Plan.Name, quota, used, quota == 0, true);
    }

    private Task<StudentSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _subscriptionRepository.GetCurrentForStudentAsync(userId, DateTime.UtcNow, cancellationToken);
    }

    private static string PeriodKey(Guid subscriptionId)
    {
        return $"SUB-{subscriptionId:N}";
    }

    private static string NormalizeSessionTitle(string title)
    {
        var clean = string.Join(' ', title.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw new InvalidOperationException("Session title cannot be empty.");
        }

        return clean.Length <= 160 ? clean : clean[..160];
    }

    private static ChatMessageDto ToDto(ChatMessage message, double? processingSeconds = null)
    {
        var citations = string.IsNullOrWhiteSpace(message.CitationsJson)
            ? []
            : JsonSerializer.Deserialize<List<CitationDto>>(message.CitationsJson) ?? [];

        return new ChatMessageDto(
            message.Id,
            message.Role == ChatRole.User ? "user" : "assistant",
            message.Content,
            citations,
            message.Error,
            message.CreatedAtUtc,
            processingSeconds);
    }
}
