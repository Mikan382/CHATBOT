using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public interface IChatRepository
{
    Task<ChatSession> EnsureSessionAsync(Guid sessionId, Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task<ChatSession?> GetOwnedSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> ListMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken);
    Task UpdateSessionTitleFromFirstUserMessageAsync(Guid sessionId, Guid userId, string messageText, CancellationToken cancellationToken);
    Task AddAssistantMessageAndTouchSessionAsync(ChatMessage message, Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task ClearMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> ListRecentMessagesAsync(Guid sessionId, Guid userId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatSession>> ListSessionsAsync(Guid userId, int take, CancellationToken cancellationToken);
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _db;

    public ChatRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSession> EnsureSessionAsync(Guid sessionId, Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is not null)
        {
            if (session.CourseId != courseId)
            {
                session.CourseId = courseId;
                await _db.SaveChangesAsync(cancellationToken);
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
            .Include(x => x.Course)
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

    public async Task AddAssistantMessageAndTouchSessionAsync(ChatMessage message, Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        _db.ChatMessages.Add(message);

        var session = await _db.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is not null)
        {
            session.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId && x.ChatSession!.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
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

    public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(Guid userId, int take, CancellationToken cancellationToken)
    {
        return await _db.ChatSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
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
