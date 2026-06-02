using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Data;
using Prn222Chatbot.Web.Domain;

namespace Prn222Chatbot.Web.Repositories;

public interface IChatRepository
{
    Task EnsureSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> ListMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken);
    Task AddAssistantMessageAndTouchSessionAsync(ChatMessage message, Guid sessionId, CancellationToken cancellationToken);
    Task ClearMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> ListRecentMessagesAsync(Guid sessionId, int take, CancellationToken cancellationToken);
}

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _db;

    public ChatRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var exists = await _db.ChatSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.ChatSessions.Add(new ChatSession
        {
            Id = sessionId,
            Title = $"PRN222 Session {DateTime.Now:HH:mm dd/MM}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId)
            .OrderBy(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAssistantMessageAndTouchSessionAsync(ChatMessage message, Guid sessionId, CancellationToken cancellationToken)
    {
        _db.ChatMessages.Add(message);

        var session = await _db.ChatSessions.FindAsync([sessionId], cancellationToken);
        if (session is not null)
        {
            session.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListRecentMessagesAsync(Guid sessionId, int take, CancellationToken cancellationToken)
    {
        return await _db.ChatMessages
            .Where(x => x.ChatSessionId == sessionId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .OrderBy(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
