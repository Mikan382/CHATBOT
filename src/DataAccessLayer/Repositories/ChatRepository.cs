using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _db;

    public ChatRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSession> EnsureSessionAsync(Guid sessionId, Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is not null)
        {
            if (session.UserId != userId)
            {
                throw new InvalidOperationException("Chat session was not found.");
            }

            if (session.CourseId != courseId)
            {
                throw new InvalidOperationException("A saved chat session cannot change course.");
            }

            return session;
        }

        session = new ChatSession
        {
            Id = sessionId,
            UserId = userId,
            CourseId = courseId,
            Title = "New chat",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<ChatSession?> GetOwnedSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        return await _db.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        return await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId && x.ChatSession!.UserId == userId)
            .OrderBy(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSessionTitleFromFirstUserMessageAsync(Guid sessionId, Guid userId, string messageText, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null || session.Title != "New chat")
        {
            return;
        }

        var userMessageCount = await _db.ChatMessages
            .CountAsync(x => x.ChatSessionId == sessionId && x.Role == ChatRole.User, cancellationToken);
        if (userMessageCount != 1)
        {
            return;
        }

        session.Title = TruncateSessionTitle(messageText);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAssistantMessageAndTouchSessionAsync(
        ChatMessage message,
        Guid sessionId,
        Guid userId,
        ChatTokenUsageUpdate? tokenUsage,
        CancellationToken cancellationToken)
    {
        if (tokenUsage is null)
        {
            _db.ChatMessages.Add(message);
            var currentSession = await _db.ChatSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
            if (currentSession is not null)
            {
                currentSession.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (tokenUsage.InputTokens < 0 || tokenUsage.OutputTokens < 0 || tokenUsage.TotalTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenUsage), "Token usage must contain non-negative counts and a positive total.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var updatedSubscription = await _db.StudentSubscriptions
            .Where(x => x.Id == tokenUsage.StudentSubscriptionId && x.StudentUserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.InputTokensUsed, x => x.InputTokensUsed + tokenUsage.InputTokens)
                .SetProperty(x => x.OutputTokensUsed, x => x.OutputTokensUsed + tokenUsage.OutputTokens)
                .SetProperty(x => x.TotalTokensUsed, x => x.TotalTokensUsed + tokenUsage.TotalTokens)
                .SetProperty(x => x.UpdatedAtUtc, _ => DateTime.UtcNow), cancellationToken);
        if (updatedSubscription == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("The subscription used for this chat request was not found.");
        }

        _db.ChatMessages.Add(message);
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is not null)
        {
            session.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ClearMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var messages = await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId && x.ChatSession!.UserId == userId)
            .ToListAsync(cancellationToken);
        _db.ChatMessages.RemoveRange(messages);
        session.Title = "New chat";
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListRecentMessagesAsync(Guid sessionId, Guid userId, int take, CancellationToken cancellationToken)
    {
        return await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId && x.ChatSession!.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .OrderBy(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(
        Guid userId,
        string? searchTerm,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _db.ChatSessions.Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Messages.Any(m => m.Content.Contains(term)));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RenameSessionAsync(Guid sessionId, Guid userId, string title, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null)
        {
            return false;
        }

        session.Title = title;
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null)
        {
            return false;
        }

        _db.ChatSessions.Remove(session);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string TruncateSessionTitle(string text)
    {
        var clean = text.Trim().Replace('\n', ' ').Replace('\r', ' ');
        return clean.Length <= 50 ? clean : clean[..47] + "...";
    }
}
