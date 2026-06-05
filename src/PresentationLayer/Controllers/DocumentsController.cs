using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.All)]
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
        Guid? courseId,
        Guid? chapterId,
        DocumentIndexStatus? status,
        CancellationToken cancellationToken)
    {
        return await BuildIndexViewAsync(searchTerm, courseId, chapterId, status, cancellationToken);
    }

    [HttpPost("/documents/upload")]
    [Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(UploadDocumentInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await BuildIndexViewAsync(null, null, null, null, cancellationToken);
        }

        try
        {
            await _documentService.UploadAsync(input.ChapterId!.Value, CurrentUserId(), input.File!, cancellationToken);
            TempData["Success"] = "Document uploaded. The worker will prepare it in the background.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("/documents/{id:guid}/delete")]
    [Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
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
            TempData["Success"] = "Document and related indexed sections were deleted.";
            return RedirectToAction(nameof(Index), new
            {
                searchTerm,
                chapterId
            });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new
            {
                searchTerm,
                chapterId
            });
        }
    }

    [HttpGet("/api/documents")]
    public async Task<IActionResult> ApiDocuments(CancellationToken cancellationToken)
    {
        var documents = await _documentService.ListDocumentsAsync(cancellationToken);
        return Json(new { success = true, documents });
    }

    [HttpGet("/documents/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentService.GetDetailsAsync(id, cancellationToken);
            return View(new DocumentDetailsViewModel
            {
                Document = document,
                CanManageDocuments = User.IsInRole(UserRoleNames.Teacher) || User.IsInRole(UserRoleNames.Admin)
            });
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("/api/documents/{id:guid}/chunks")]
    public async Task<IActionResult> ApiChunks(Guid id, CancellationToken cancellationToken)
    {
        var chunks = await _documentService.ListChunksAsync(id, cancellationToken);
        return Json(new { success = true, chunks });
    }

    private async Task<IActionResult> BuildIndexViewAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        DocumentIndexStatus? status,
        CancellationToken cancellationToken)
    {
        var data = await _documentService.GetIndexDataAsync(searchTerm, courseId, chapterId, status, cancellationToken);
        return View("Index", new DocumentIndexViewModel
        {
            Chapters = data.Chapters,
            Courses = await _documentService.ListCoursesAsync(cancellationToken),
            Documents = data.Documents,
            SearchTerm = searchTerm,
            SelectedCourseId = courseId,
            SelectedChapterId = chapterId,
            SelectedStatus = status,
            CanManageDocuments = User.IsInRole(UserRoleNames.Teacher) || User.IsInRole(UserRoleNames.Admin)
        });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }
}
