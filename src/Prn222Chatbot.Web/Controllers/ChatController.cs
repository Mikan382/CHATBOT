using Microsoft.AspNetCore.Mvc;
using Prn222Chatbot.Web.Services;
using Prn222Chatbot.Web.ViewModels;

namespace Prn222Chatbot.Web.Controllers;

public class ChatController : Controller
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("/chat")]
    public IActionResult Index(Guid? sessionId)
    {
        return View(new ChatIndexViewModel
        {
            SessionId = sessionId ?? Guid.NewGuid(),
            FineTuneConfigured = _chatService.FineTuneConfigured,
            GeminiConfigured = _chatService.GeminiConfigured
        });
    }

    [HttpGet("/api/chat/{sessionId:guid}")]
    public async Task<IActionResult> History(Guid sessionId, CancellationToken cancellationToken)
    {
        return Json(new { success = true, history = await _chatService.GetHistoryAsync(sessionId, cancellationToken) });
    }
}
