using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize]
[RequestSizeLimit(100 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
public class DocumentsController : BaseController
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        string? status,
        CancellationToken cancellationToken)
    {
        var data = await _documentService.GetIndexDataAsync(searchTerm, courseId, chapterId, status, cancellationToken);
        var courses = await _documentService.ListCoursesAsync(cancellationToken);

        var model = new DocumentIndexViewModel
        {
            Chapters = data.Chapters,
            Courses = courses,
            Documents = data.Documents,
            SearchTerm = searchTerm,
            SelectedCourseId = courseId,
            SelectedChapterId = chapterId,
            SelectedStatus = status,
            CanManageDocuments = User.IsInRole("Teacher") || User.IsInRole("Admin"),
            StatusOptions = ["Pending", "Processing", "Indexed", "Failed"]
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var details = await _documentService.GetDetailsAsync(id, cancellationToken);
            return View(details);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid chapterId, IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            await _documentService.UploadAsync(chapterId, CurrentUserId(), stream, file.FileName, file.Length, cancellationToken);
            SetFlashSuccess("Document uploaded. The worker will prepare it in the background.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? searchTerm, Guid? chapterId, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.DeleteAsync(id, cancellationToken);
            SetFlashSuccess("Document and related indexed sections were deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index", new { searchTerm, chapterId });
    }
}
