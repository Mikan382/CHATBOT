using System.Diagnostics;
using System.Text.Json;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Services;

public class ChatService : IChatService
{
    private const double MinCitationScore = 0.36;
    private readonly IChatRepository _chatRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly RetrievalService _retrievalService;
    private readonly IGeminiClient _geminiClient;

    public ChatService(
        IChatRepository chatRepository,
        ICourseRepository courseRepository,
        RetrievalService retrievalService,
        IGeminiClient geminiClient)
    {
        _chatRepository = chatRepository;
        _courseRepository = courseRepository;
        _retrievalService = retrievalService;
        _geminiClient = geminiClient;
    }

    public bool GeminiConfigured => _geminiClient.IsConfigured;

    public async Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _chatRepository.GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session is null) return null;
        return new ChatSessionDto(session.Id, session.CourseId);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _chatRepository.GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session is null)
        {
            return [];
        }

        var messages = await _chatRepository.ListMessagesAsync(sessionId, userId, cancellationToken);
        return messages.Select(m => ToDto(m)).ToList();
    }

    public async Task<ChatMessageDto> SaveUserMessageAsync(Guid sessionId, Guid userId, Guid courseId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Message is empty.");
        }

        _ = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        await _chatRepository.EnsureSessionAsync(sessionId, userId, courseId, cancellationToken);

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
        return ToDto(userMessage);
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
            answer = ConversationalMessageDetector.IsConversationalOnly(text)
                ? await GenerateConversationalAsync(text, course.Name, cancellationToken)
                : await GenerateRagAsync(course.Id, course.Name, text, history, citations, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            answer = $"Could not generate an answer: {ex.Message}";
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

    public async Task<ChatResponseDto> SendAsync(Guid sessionId, Guid userId, Guid courseId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Message is empty.");
        }

        var trimmed = text.Trim();
        var userDto = await SaveUserMessageAsync(sessionId, userId, courseId, trimmed, cancellationToken);
        var botDto = await GenerateAssistantReplyAsync(sessionId, userId, courseId, trimmed, cancellationToken);
        return new ChatResponseDto(userDto, botDto);
    }

    public async Task ClearAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await _chatRepository.ClearMessagesAsync(sessionId, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<SessionListDto>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await _chatRepository.ListSessionsAsync(userId, 20, cancellationToken);
        return sessions.Select(s => new SessionListDto(s.Id, s.Title, s.UpdatedAtUtc)).ToList();
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        return await _chatRepository.DeleteSessionAsync(sessionId, userId, cancellationToken);
    }

    private async Task<string> GenerateConversationalAsync(string text, string courseName, CancellationToken cancellationToken)
    {
        return await _geminiClient.GenerateAsync(
            ConversationalPromptBuilder.BuildSystemInstruction(courseName),
            text.Trim(),
            cancellationToken);
    }

    private async Task<string> GenerateRagAsync(
        Guid courseId,
        string courseName,
        string text,
        IReadOnlyList<ChatHistoryMessage> history,
        List<CitationDto> citations,
        CancellationToken cancellationToken)
    {
        var chunks = FilterRelevantChunks(await _retrievalService.RetrieveAsync(text, courseId, 3, cancellationToken));
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return "The indexed documents do not contain enough relevant information to answer this question accurately.";
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        return await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(courseName), prompt, cancellationToken);
    }

    private static IReadOnlyList<RetrievedChunkDto> FilterRelevantChunks(IReadOnlyList<RetrievedChunkDto> chunks)
    {
        return chunks.Where(chunk => chunk.Score >= MinCitationScore).ToList();
    }

    private async Task<IReadOnlyList<ChatHistoryMessage>> BuildHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken, bool excludeLatestUserMessage = false)
    {
        var messages = await _chatRepository.ListRecentMessagesAsync(sessionId, userId, 12, cancellationToken);
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
