using System.Text.Json;
using Prn222Chatbot.Web.Domain;
using Prn222Chatbot.Web.Domain.Enums;
using Prn222Chatbot.Web.Repositories;
using Prn222Chatbot.Web.Services.Ai;
using Prn222Chatbot.Web.Services.Retrieval;

namespace Prn222Chatbot.Web.Services;

public class ChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly RetrievalService _retrievalService;
    private readonly IGeminiClient _geminiClient;
    private readonly IFineTuneClient _fineTuneClient;

    public ChatService(IChatRepository chatRepository, RetrievalService retrievalService, IGeminiClient geminiClient, IFineTuneClient fineTuneClient)
    {
        _chatRepository = chatRepository;
        _retrievalService = retrievalService;
        _geminiClient = geminiClient;
        _fineTuneClient = fineTuneClient;
    }

    public bool GeminiConfigured => _geminiClient.IsConfigured;

    public bool FineTuneConfigured => _fineTuneClient.IsConfigured;

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await _chatRepository.EnsureSessionAsync(sessionId, cancellationToken);
        var messages = await _chatRepository.ListMessagesAsync(sessionId, cancellationToken);
        return messages.Select(ToDto).ToList();
    }

    public async Task<ChatResponseDto> SendAsync(Guid sessionId, ModelType modelType, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Message is empty.");
        }

        await _chatRepository.EnsureSessionAsync(sessionId, cancellationToken);
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

        var history = await BuildHistoryAsync(sessionId, cancellationToken);
        var citations = new List<CitationDto>();
        string answer;
        string? error = null;

        try
        {
            answer = modelType switch
            {
                ModelType.FineTunedOnly => await GenerateFineTunedAsync(sessionId, text, history, cancellationToken),
                ModelType.RagHybrid => await GenerateHybridAsync(sessionId, text, history, citations, cancellationToken),
                _ => await GenerateRagAsync(text, history, citations, cancellationToken)
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

        await _chatRepository.AddAssistantMessageAndTouchSessionAsync(botMessage, sessionId, cancellationToken);
        return new ChatResponseDto(ToDto(userMessage), ToDto(botMessage));
    }

    public async Task ClearAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await _chatRepository.ClearMessagesAsync(sessionId, cancellationToken);
    }

    private async Task<string> GenerateRagAsync(string text, IReadOnlyList<FineTuneHistoryMessage> history, List<CitationDto> citations, CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveAsync(text, 3, cancellationToken);
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return "The indexed documents do not contain enough relevant information to answer this question accurately.";
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        return await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
    }

    private async Task<string> GenerateFineTunedAsync(Guid sessionId, string text, IReadOnlyList<FineTuneHistoryMessage> history, CancellationToken cancellationToken)
    {
        var response = await _fineTuneClient.GenerateAsync(new FineTuneRequest(sessionId.ToString(), "PRN222", text, history), cancellationToken);
        return response.Answer;
    }

    private async Task<string> GenerateHybridAsync(Guid sessionId, string text, IReadOnlyList<FineTuneHistoryMessage> history, List<CitationDto> citations, CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveAsync(text, 3, cancellationToken);
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return await GenerateFineTunedAsync(sessionId, text, history, cancellationToken);
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        return await _geminiClient.GenerateAsync(RagPromptBuilder.BuildSystemInstruction(), prompt, cancellationToken);
    }

    private async Task<IReadOnlyList<FineTuneHistoryMessage>> BuildHistoryAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var messages = await _chatRepository.ListRecentMessagesAsync(sessionId, 12, cancellationToken);
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
