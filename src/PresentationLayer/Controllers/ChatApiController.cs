using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;

namespace PresentationLayer.Controllers;

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
    public async Task<IActionResult> Sessions(string? q, CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _chatService.ListSessionsAsync(CurrentUserId(), q, cancellationToken);
            return Ok(new { success = true, sessions });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPatch("/api/chat/{sessionId:guid}/title")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameSession(Guid sessionId, RenameSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedTitle = await _chatService.RenameSessionAsync(
                sessionId,
                CurrentUserId(),
                request.Title ?? "",
                cancellationToken);
            return normalizedTitle is not null
                ? Ok(new { success = true, title = normalizedTitle })
                : NotFound(new { success = false, error = "Session was not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpDelete("/api/chat/{sessionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var deleted = await _chatService.DeleteSessionAsync(sessionId, CurrentUserId(), cancellationToken);
        return deleted
            ? Ok(new { success = true })
            : NotFound(new { success = false, error = "Session was not found." });
    }

    [HttpPost("/api/chat/{sessionId:guid}/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadStudentDocument(
        Guid sessionId,
        [FromForm] Guid courseId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, error = "Please select a valid file to upload." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _chatService.UploadStudentDocumentAsync(
                sessionId,
                CurrentUserId(),
                courseId,
                stream,
                file.FileName,
                file.Length,
                cancellationToken);
            return Ok(new { success = true, document = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("/api/chat/{sessionId:guid}/documents")]
    public async Task<IActionResult> ListStudentDocuments(Guid sessionId, CancellationToken cancellationToken)
    {
        var documents = await _chatService.ListStudentDocumentsAsync(sessionId, CurrentUserId(), cancellationToken);
        return Ok(new { success = true, documents });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }
}

public record RenameSessionRequest(string? Title);
