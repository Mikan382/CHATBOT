using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BusinessLayer.Services;

namespace PresentationLayer.ApiControllers;

[ApiController]
[Authorize]
public class ChatApiController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatApiController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("/api/chat/{sessionId:guid}")]
    public async Task<IActionResult> History(Guid sessionId, CancellationToken cancellationToken)
    {
        return Ok(new { success = true, history = await _chatService.GetHistoryAsync(sessionId, CurrentUserId(), cancellationToken) });
    }

    [HttpGet("/api/chat/sessions")]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    {
        var sessions = await _chatService.ListSessionsAsync(CurrentUserId(), cancellationToken);
        return Ok(new { success = true, sessions });
    }

    [HttpDelete("/api/chat/{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var deleted = await _chatService.DeleteSessionAsync(sessionId, CurrentUserId(), cancellationToken);
        return Ok(new { success = deleted });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }
}
