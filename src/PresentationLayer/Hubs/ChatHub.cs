using Microsoft.AspNetCore.SignalR;
using BusinessLayer.Services;

namespace PresentationLayer.Hubs;

public class ChatHub : Hub
{
    private readonly ChatService _chatService;

    public ChatHub(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task JoinSession(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out _))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task SendMessage(string sessionId, string modelType, string text)
    {
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
            return;
        }

        if (!ModelTypeParser.TryParse(modelType, out var parsedModelType))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid model type.");
            return;
        }

        try
        {
            var response = await _chatService.SendAsync(parsedSessionId, parsedModelType, text, Context.ConnectionAborted);
            await Clients.Group(sessionId).SendAsync("MessageReceived", response.UserMessage);
            await Clients.Group(sessionId).SendAsync("MessageReceived", response.BotMessage);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("MessageFailed", ex.Message);
        }
    }

    public async Task ClearSession(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Clients.Caller.SendAsync("MessageFailed", "Invalid session ID.");
            return;
        }

        await _chatService.ClearAsync(parsedSessionId, Context.ConnectionAborted);
        await Clients.Group(sessionId).SendAsync("SessionCleared");
    }
}
