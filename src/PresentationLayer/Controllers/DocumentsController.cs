using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize]
[RequestSizeLimit(DocumentUploadLimits.MaxRequestBodyBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = DocumentUploadLimits.MaxRequestBodyBytes)]
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
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");
        var data = await _documentService.GetIndexDataAsync(searchTerm, courseId, chapterId, CurrentUserId(), isAdmin, isTeacher, cancellationToken);
        var courses = await _documentService.ListCoursesAsync(CurrentUserId(), isAdmin, isTeacher, cancellationToken);

        var model = new DocumentIndexViewModel
        {
            Chapters = data.Chapters,
            Courses = courses,
            Documents = data.Documents,
            SearchTerm = searchTerm,
            SelectedCourseId = courseId,
            SelectedChapterId = chapterId,
            CanManageDocuments = isTeacher || isAdmin
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
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid chapterId, IFormFile? file, CancellationToken cancellationToken)
    {
        try
        {
            if (file is null)
            {
                throw new InvalidOperationException("Please select a file to upload.");
            }

            await using var stream = file.OpenReadStream();
            await _documentService.UploadAsync(chapterId, CurrentUserId(), User.IsInRole("Admin"), stream, file.FileName, file.Length, cancellationToken);
            SetFlashSuccess("Document uploaded and indexed.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
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
            await _documentService.DeleteAsync(id, CurrentUserId(), User.IsInRole("Admin"), cancellationToken);
            SetFlashSuccess("Document and related indexed sections were deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(UserFacingError(ex));
        }

        return RedirectToAction("Index", new { searchTerm, chapterId });
    }
}
