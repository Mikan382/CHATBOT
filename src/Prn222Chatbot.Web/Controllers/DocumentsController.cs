using Microsoft.AspNetCore.Mvc;
using Prn222Chatbot.Web.Services;
using Prn222Chatbot.Web.ViewModels;

namespace Prn222Chatbot.Web.Controllers;

public class DocumentsController : Controller
{
    private readonly DocumentService _documentService;

    public DocumentsController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("/documents")]
    public async Task<IActionResult> Index(
        string? searchTerm,
        Guid? chapterId,
        string? message,
        string? error,
        CancellationToken cancellationToken)
    {
        return await BuildIndexViewAsync(message, error, searchTerm, chapterId, cancellationToken);
    }

    [HttpPost("/documents/upload")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(UploadDocumentInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await BuildIndexViewAsync(null, ValidationSummary(), null, null, cancellationToken);
        }

        try
        {
            await _documentService.UploadAsync(input.ChapterId!.Value, input.File!, cancellationToken);
            return RedirectToAction(nameof(Index), new { message = "Document uploaded. The worker will index it in the background." });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Index), new { error = ex.Message });
        }
    }

    [HttpPost("/documents/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid id,
        string? searchTerm,
        Guid? chapterId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.DeleteAsync(id, cancellationToken);
            return RedirectToAction(nameof(Index), new
            {
                searchTerm,
                chapterId,
                message = "Document and related chunks were deleted."
            });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Index), new
            {
                searchTerm,
                chapterId,
                error = ex.Message
            });
        }
    }

    [HttpGet("/api/documents")]
    public async Task<IActionResult> ApiDocuments(CancellationToken cancellationToken)
    {
        var documents = await _documentService.ListDocumentsAsync(cancellationToken);
        return Json(new { success = true, documents });
    }

    [HttpGet("/api/documents/{id:guid}/chunks")]
    public async Task<IActionResult> ApiChunks(Guid id, CancellationToken cancellationToken)
    {
        var chunks = await _documentService.ListChunksAsync(id, cancellationToken);
        return Json(new { success = true, chunks });
    }

    private async Task<IActionResult> BuildIndexViewAsync(
        string? message,
        string? error,
        string? searchTerm,
        Guid? chapterId,
        CancellationToken cancellationToken)
    {
        var data = await _documentService.GetIndexDataAsync(searchTerm, chapterId, cancellationToken);
        return View("Index", new DocumentIndexViewModel
        {
            Chapters = data.Chapters,
            Documents = data.Documents,
            SearchTerm = searchTerm,
            SelectedChapterId = chapterId,
            Message = message,
            Error = error
        });
    }

    private string ValidationSummary()
    {
        return string.Join(" ", ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
