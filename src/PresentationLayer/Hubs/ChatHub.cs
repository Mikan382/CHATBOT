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
            if (IsStudent())
            {
                var quota = await _chatService.GetQuotaStatusAsync(userId, Context.ConnectionAborted);
                if (quota.Exhausted)
                {
                    await Clients.Caller.SendAsync(
                        "MessageFailed",
                        $"You have used all {quota.Quota} messages of the {quota.PlanName} package. Please register or upgrade your package to continue.");
                    return;
                }
            }

            // Once the send is accepted, persist the user message, quota, and assistant reply
            // independently of the connection. If the client navigates away or refreshes, the
            // SignalR connection aborts; tying persistence to it would cancel generation mid-way
            // and lose the reply (while quota was already spent). CancellationToken.None keeps
            // the work running so the reply is saved and shows up after a reload.
            var persistToken = CancellationToken.None;

            var userMessage = await _chatService.SaveUserMessageAsync(
                parsedSessionId,
                userId,
                parsedCourseId,
                text,
                persistToken);
            if (IsStudent())
            {
                // Meter usage once the message is persisted. This counter is never decremented,
                // so deleting or clearing a session cannot refund quota.
                await _chatService.RegisterMessageUsageAsync(userId, persistToken);
            }
            await Clients.Group(SessionGroup(parsedSessionId)).SendAsync("MessageReceived", userMessage);

            var botMessage = await _chatService.GenerateAssistantReplyAsync(
                parsedSessionId,
                userId,
                parsedCourseId,
                text,
                persistToken);
            await Clients.Group(SessionGroup(parsedSessionId)).SendAsync("MessageReceived", botMessage);
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
