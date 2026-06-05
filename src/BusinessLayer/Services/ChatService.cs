using System.Text.Json;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Retrieval;

namespace BusinessLayer.Services;

public class ChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly RetrievalService _retrievalService;
    private readonly IGeminiClient _geminiClient;
    private readonly IFineTuneClient _fineTuneClient;

    public ChatService(IChatRepository chatRepository, ICourseRepository courseRepository, RetrievalService retrievalService, IGeminiClient geminiClient, IFineTuneClient fineTuneClient)
    {
        _chatRepository = chatRepository;
        _courseRepository = courseRepository;
        _retrievalService = retrievalService;
        _geminiClient = geminiClient;
        _fineTuneClient = fineTuneClient;
    }

    public bool GeminiConfigured => _geminiClient.IsConfigured;

    public bool FineTuneConfigured => _fineTuneClient.IsConfigured;

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _chatRepository.GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session is null)
        {
            return [];
        }

        var messages = await _chatRepository.ListMessagesAsync(sessionId, userId, cancellationToken);
        return messages.Select(ToDto).ToList();
    }

    public async Task<ChatResponseDto> SendAsync(Guid sessionId, Guid userId, Guid courseId, ModelType modelType, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Message is empty.");
        }

        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        await _chatRepository.EnsureSessionAsync(sessionId, userId, courseId, cancellationToken);
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = ChatRole.User,
            ModelType = modelType,
            Content = text.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _chatRepository.AddMessageAsync(userMessage, cancellationToken);

        var history = await BuildHistoryAsync(sessionId, userId, cancellationToken);
        var citations = new List<CitationDto>();
        string answer;
        string? error = null;

        try
        {
            answer = modelType switch
            {
                ModelType.FineTunedOnly => await GenerateFineTunedAsync(sessionId, course.Code, text, history, cancellationToken),
                ModelType.RagHybrid => await GenerateHybridAsync(sessionId, course.Id, course.Code, text, history, citations, cancellationToken),
                _ => await GenerateRagAsync(course.Id, text, history, citations, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            error = ex.Message;
            answer = $"Could not generate an answer: {ex.Message}";
        }

        var botMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = ChatRole.Assistant,
            ModelType = modelType,
            Content = answer,
            CitationsJson = citations.Count > 0 ? JsonSerializer.Serialize(citations) : null,
            Error = error,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _chatRepository.AddAssistantMessageAndTouchSessionAsync(botMessage, sessionId, userId, cancellationToken);
        return new ChatResponseDto(ToDto(userMessage), ToDto(botMessage));
    }

    public async Task ClearAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await _chatRepository.ClearMessagesAsync(sessionId, userId, cancellationToken);
    }

    private async Task<string> GenerateRagAsync(Guid courseId, string text, IReadOnlyList<FineTuneHistoryMessage> history, List<CitationDto> citations, CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveAsync(text, courseId, 3, cancellationToken);
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return "The indexed documents do not contain enough relevant information to answer this question accurately.";
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        return await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
    }

    private async Task<string> GenerateFineTunedAsync(Guid sessionId, string courseCode, string text, IReadOnlyList<FineTuneHistoryMessage> history, CancellationToken cancellationToken)
    {
        var response = await _fineTuneClient.GenerateAsync(new FineTuneRequest(sessionId.ToString(), courseCode, text, history), cancellationToken);
        return response.Answer;
    }

    private async Task<string> GenerateHybridAsync(Guid sessionId, Guid courseId, string courseCode, string text, IReadOnlyList<FineTuneHistoryMessage> history, List<CitationDto> citations, CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveAsync(text, courseId, 3, cancellationToken);
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return await GenerateFineTunedAsync(sessionId, courseCode, text, history, cancellationToken);
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        return await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
    }

    private async Task<IReadOnlyList<FineTuneHistoryMessage>> BuildHistoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var messages = await _chatRepository.ListRecentMessagesAsync(sessionId, userId, 12, cancellationToken);
        return messages
            .Select(x => new FineTuneHistoryMessage(x.Role == ChatRole.User ? "user" : "assistant", x.Content))
            .ToList();
    }

    private static CitationDto ToCitation(RetrievedChunkDto chunk)
    {
        return new CitationDto(chunk.ChunkId, chunk.SourceName, chunk.ChapterTitle, chunk.ChunkIndex, chunk.Content);
    }

    private static ChatMessageDto ToDto(ChatMessage message)
    {
        var citations = string.IsNullOrWhiteSpace(message.CitationsJson)
            ? []
            : JsonSerializer.Deserialize<List<CitationDto>>(message.CitationsJson) ?? [];

        return new ChatMessageDto(
            message.Id,
            message.Role == ChatRole.User ? "user" : "assistant",
            message.ModelType.ToClientValue(),
            message.Content,
            citations,
            message.Error,
            message.CreatedAtUtc);
    }
}
