using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BusinessLayer.Services;

namespace PresentationLayer.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IAuthService _authService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, IAuthService authService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _authService = authService;
        _logger = logger;
    }

    public async Task JoinSession(string sessionId)
    {
        if (!await EnsureCurrentPrincipalAsync()) return;
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(parsedSessionId));
    }

    public async Task LeaveSession(string sessionId)
    {
        if (!await EnsureCurrentPrincipalAsync()) return;
        if (Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(parsedSessionId));
        }
    }

    public async Task SendMessage(string sessionId, string courseId, string text)
    {
        if (!await EnsureCurrentPrincipalAsync()) return;
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
            return;
        }

        if (!Guid.TryParse(courseId, out var parsedCourseId))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid course ID.");
            return;
        }

        try
        {
            var userId = CurrentUserId();
            // Once accepted, keep generation independent of the connection so a refresh does not
            // lose the persisted reply. Token usage is recorded only after Gemini succeeds.
            var persistToken = CancellationToken.None;
            var accepted = await _chatService.AcceptUserMessageAsync(
                parsedSessionId,
                userId,
                parsedCourseId,
                text,
                IsStudent(),
                persistToken);
            if (accepted.Quota is not null)
            {
                await Clients.Caller.SendAsync("QuotaUpdated", accepted.Quota);
            }

            await Clients.Group(SessionGroup(parsedSessionId)).SendAsync("MessageReceived", accepted.Message);

            var botMessage = await _chatService.GenerateAssistantReplyAsync(
                parsedSessionId,
                userId,
                parsedCourseId,
                text,
                accepted.StudentSubscriptionId,
                persistToken);
            await Clients.Group(SessionGroup(parsedSessionId)).SendAsync("MessageReceived", botMessage);
            if (IsStudent())
            {
                var quota = await _chatService.GetQuotaStatusAsync(userId, persistToken);
                await Clients.Caller.SendAsync("QuotaUpdated", quota);
            }
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("MessageFailed", ex.Message);
        }
        catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat message failed for session {SessionId}.", parsedSessionId);
            await Clients.Caller.SendAsync("MessageFailed", "The assistant could not process the message.");
        }
    }

    public async Task ClearSession(string sessionId)
    {
        if (!await EnsureCurrentPrincipalAsync()) return;
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
            return;
        }

        try
        {
            await _chatService.ClearAsync(parsedSessionId, CurrentUserId(), Context.ConnectionAborted);
            await Clients.Group(SessionGroup(parsedSessionId)).SendAsync("SessionCleared");
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("MessageFailed", ex.Message);
        }
    }

    private Guid CurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }

    private bool IsStudent()
    {
        return string.Equals(Context.User?.FindFirstValue(ClaimTypes.Role), "Student", StringComparison.OrdinalIgnoreCase);
    }

    private string SessionGroup(Guid sessionId)
    {
        return $"chat:{CurrentUserId():N}:{sessionId:N}";
    }

    private async Task<bool> EnsureCurrentPrincipalAsync()
    {
        var idValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = Context.User?.FindFirstValue(ClaimTypes.Email);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        var versionValue = Context.User?.FindFirstValue("user_version");
        var valid = Guid.TryParse(idValue, out var userId)
            && long.TryParse(versionValue, out var userVersion)
            && !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(role)
            && await _authService.IsPrincipalCurrentAsync(
                userId,
                email,
                role,
                userVersion,
                Context.ConnectionAborted);
        if (valid)
        {
            return true;
        }

        await Clients.Caller.SendAsync("MessageFailed", "Your session has expired. Please sign in again.");
        Context.Abort();
        return false;
    }
}
