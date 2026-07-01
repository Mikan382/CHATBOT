using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.Documents;

[Authorize(Roles = UserRoleNames.All)]
[RequestSizeLimit(100 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
public class IndexModel : PageModel
{
    private readonly DocumentService _documentService;

    public IndexModel(DocumentService documentService)
    {
        _documentService = documentService;
    }

    public IReadOnlyList<Chapter> ChapterList { get; set; } = [];
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public IReadOnlyList<Document> DocumentList { get; set; } = [];
    public string? SearchTerm { get; set; }
    public Guid? SelectedCourseId { get; set; }
    public Guid? SelectedChapterId { get; set; }
    public DocumentIndexStatus? SelectedStatus { get; set; }
    public bool CanManageDocuments { get; set; }

    [BindProperty]
    public UploadDocumentInput Upload { get; set; } = new();

    public async Task OnGetAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        DocumentIndexStatus? status,
        CancellationToken cancellationToken)
    {
        await LoadDataAsync(searchTerm, courseId, chapterId, status, cancellationToken);
    }

    [Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostUploadAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync(null, null, null, null, cancellationToken);
            return Page();
        }

        try
        {
            await _documentService.UploadAsync(Upload.ChapterId!.Value, CurrentUserId(), Upload.File!, cancellationToken);
            TempData["Success"] = "Document uploaded. The worker will prepare it in the background.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage();
        }
    }

    [Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(
        Guid id,
        string? searchTerm,
        Guid? chapterId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Document and related indexed sections were deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { searchTerm, chapterId });
    }

    private async Task LoadDataAsync(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        DocumentIndexStatus? status,
        CancellationToken cancellationToken)
    {
        SearchTerm = searchTerm;
        SelectedCourseId = courseId;
        SelectedChapterId = chapterId;
        SelectedStatus = status;
        CanManageDocuments = User.IsInRole(UserRoleNames.Teacher) || User.IsInRole(UserRoleNames.Admin);

        var data = await _documentService.GetIndexDataAsync(searchTerm, courseId, chapterId, status, cancellationToken);
        ChapterList = data.Chapters;
        Courses = await _documentService.ListCoursesAsync(cancellationToken);
        DocumentList = data.Documents;
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user ID is invalid.");
    }
}
