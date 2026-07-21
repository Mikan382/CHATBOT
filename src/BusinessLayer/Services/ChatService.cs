using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BusinessLayer.AI;
using BusinessLayer.DTOs;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;
using BusinessLayer.Retrieval;
using DataAccessLayer.Data;
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
    private readonly IDocumentTextExtractor _extractor;
    private readonly DocumentIndexingService _indexingService;
    private readonly AppDbContext _db;
    private readonly RagOptions _ragOptions;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository chatRepository,
        ICourseRepository courseRepository,
        ISubscriptionRepository subscriptionRepository,
        RetrievalService retrievalService,
        IGeminiClient geminiClient,
        IDocumentTextExtractor extractor,
        DocumentIndexingService indexingService,
        AppDbContext db,
        IOptions<RagOptions> ragOptions,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _courseRepository = courseRepository;
        _subscriptionRepository = subscriptionRepository;
        _retrievalService = retrievalService;
        _geminiClient = geminiClient;
        _extractor = extractor;
        _indexingService = indexingService;
        _db = db;
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
            return new ChatQuotaStatusDto("No active package", 0, 0, null, false);
        }

        return ToQuotaStatus(active);
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

        StudentSubscription? subscription = null;
        if (enforceQuota)
        {
            subscription = await EnsureTokenQuotaAvailableAsync(userId, cancellationToken);
        }

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
        return new AcceptedChatMessageDto(
            ToDto(userMessage),
            subscription is null ? null : ToQuotaStatus(subscription),
            subscription?.Id);
    }

    public async Task<ChatMessageDto> GenerateAssistantReplyAsync(
        Guid sessionId,
        Guid userId,
        Guid courseId,
        string text,
        Guid? studentSubscriptionId,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        var history = await BuildHistoryAsync(sessionId, userId, cancellationToken, excludeLatestUserMessage: true);
        var citations = new List<CitationDto>();
        string answer;
        string? error = null;
        GeminiGenerationResult? generation = null;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var ragResult = await GenerateRagAsync(
                sessionId,
                course.Id,
                course.Name,
                text,
                history,
                citations,
                cancellationToken);
            answer = ragResult.Text;
            generation = ragResult.Generation;
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

        var tokenUsage = generation is not null && studentSubscriptionId.HasValue
            ? new ChatTokenUsageUpdate(
                studentSubscriptionId.Value,
                generation.InputTokens,
                generation.OutputTokens,
                generation.TotalTokens)
            : null;
        await _chatRepository.AddAssistantMessageAndTouchSessionAsync(
            botMessage,
            sessionId,
            userId,
            tokenUsage,
            cancellationToken);
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

    public async Task<StudentDocumentUploadResultDto> UploadStudentDocumentAsync(
        Guid sessionId,
        Guid userId,
        Guid courseId,
        Stream stream,
        string fileName,
        long fileSize,
        CancellationToken cancellationToken)
    {
        if (fileSize == 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        const long maxBytes = 10 * 1024 * 1024;
        if (fileSize > maxBytes)
        {
            throw new InvalidOperationException("File exceeds the 10MB limit.");
        }

        var session = await _chatRepository.EnsureSessionAsync(sessionId, userId, courseId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Chat session was not found.");
        }

        var currentDocsCount = await _db.StudentUploadedDocuments.CountAsync(x => x.ChatSessionId == sessionId, cancellationToken);
        if (currentDocsCount >= 3)
        {
            throw new InvalidOperationException("You can upload a maximum of 3 attachments per chat session.");
        }

        var text = await _extractor.ExtractAsync(stream, fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Could not extract readable text from the uploaded document.");
        }

        var contentHash = DocumentContentHasher.Compute(text);
        var existingDoc = await _db.StudentUploadedDocuments.FirstOrDefaultAsync(
            x => x.ChatSessionId == sessionId && x.ContentHash == contentHash,
            cancellationToken);
        if (existingDoc is not null)
        {
            throw new InvalidOperationException("This document has already been uploaded to this session.");
        }

        var studentDoc = new StudentUploadedDocument
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            UploadedByUserId = userId,
            OriginalFileName = fileName,
            FileType = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant(),
            FileSizeBytes = fileSize,
            ContentText = text,
            ContentHash = contentHash,
            UploadedAtUtc = DateTime.UtcNow
        };

        await _indexingService.PopulateStudentChunksAsync(studentDoc, cancellationToken);
        _db.StudentUploadedDocuments.Add(studentDoc);
        await _db.SaveChangesAsync(cancellationToken);

        return new StudentDocumentUploadResultDto(studentDoc.Id, studentDoc.OriginalFileName, studentDoc.Chunks.Count);
    }

    public async Task<IReadOnlyList<StudentDocumentDto>> ListStudentDocumentsAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var docs = await _db.StudentUploadedDocuments
            .Include(x => x.Chunks)
            .Where(x => x.ChatSessionId == sessionId && x.UploadedByUserId == userId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return docs.Select(d => new StudentDocumentDto(
            d.Id,
            d.OriginalFileName,
            d.FileType,
            d.FileSizeBytes,
            d.Chunks.Count,
            d.UploadedAtUtc)).ToList();
    }

    private async Task<RagGenerationResult> GenerateRagAsync(
        Guid sessionId,
        Guid courseId,
        string courseName,
        string text,
        IReadOnlyList<ChatHistoryMessage> history,
        List<CitationDto> citations,
        CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveAsync(text, courseId, sessionId, cancellationToken);
        citations.AddRange(chunks.Select(ToCitation));
        if (chunks.Count == 0)
        {
            return new RagGenerationResult(
                "The indexed documents do not contain enough relevant information to answer this question accurately.",
                null);
        }

        var prompt = RagPromptBuilder.BuildPrompt(text, chunks, history);
        var generation = await _geminiClient.GenerateAsync(
            RagPromptBuilder.BuildSystemInstruction(courseName),
            prompt,
            cancellationToken);
        return new RagGenerationResult(generation.Text, generation);
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

    private async Task<StudentSubscription> EnsureTokenQuotaAvailableAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var active = await GetActiveSubscriptionAsync(userId, cancellationToken);
        if (active?.Plan is null)
        {
            throw new InvalidOperationException(
                "No active subscription package is available. Ask an Admin to configure a default package.");
        }

        if (active.TotalTokensUsed >= active.TokenQuotaAtActivation)
        {
            throw new InvalidOperationException(
                $"You have used all {active.TokenQuotaAtActivation:N0} tokens in the {active.Plan.Name} package.");
        }

        return active;
    }

    private Task<StudentSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _subscriptionRepository.GetOrCreateCurrentForStudentAsync(
            userId,
            DateTime.UtcNow,
            cancellationToken);
    }

    private static ChatQuotaStatusDto ToQuotaStatus(StudentSubscription subscription)
    {
        return new ChatQuotaStatusDto(
            subscription.Plan?.Name ?? "Subscription",
            subscription.TokenQuotaAtActivation,
            subscription.TotalTokensUsed,
            subscription.ExpiresAtUtc,
            true);
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

    private sealed record RagGenerationResult(
        string Text,
        GeminiGenerationResult? Generation);
}
