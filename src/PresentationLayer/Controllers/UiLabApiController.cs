using System.Security.Claims;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[ApiController]
[Route("api/ui-lab")]
[Authorize]
public sealed class UiLabApiController : BaseController
{
    private readonly ICourseService _courseService;
    private readonly IDocumentService _documentService;
    private readonly IBenchmarkService _benchmarkService;
    private readonly IUserAdminService _userAdminService;

    public UiLabApiController(
        ICourseService courseService,
        IDocumentService documentService,
        IBenchmarkService benchmarkService,
        IUserAdminService userAdminService)
    {
        _courseService = courseService;
        _documentService = documentService;
        _benchmarkService = benchmarkService;
        _userAdminService = userAdminService;
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = CurrentUserId(),
            email = User.FindFirstValue(ClaimTypes.Email) ?? "",
            displayName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "",
            role = User.FindFirstValue(ClaimTypes.Role) ?? ""
        });
    }

    [HttpGet("courses")]
    public async Task<IActionResult> Courses(CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");
        var courses = isAdmin || isTeacher
            ? await _courseService.ListManageDtosAsync(CurrentUserId(), isAdmin, cancellationToken)
            : await _courseService.ListDtosAsync(cancellationToken);

        return Ok(new
        {
            courses,
            permissions = new { canManage = isAdmin || isTeacher, canCreate = isAdmin }
        });
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents(
        string? searchTerm,
        Guid? courseId,
        Guid? chapterId,
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");
        var page = await _documentService.GetIndexDataAsync(
            searchTerm,
            courseId,
            chapterId,
            CurrentUserId(),
            isAdmin,
            isTeacher,
            cancellationToken);

        return Ok(new
        {
            page.Courses,
            page.Chapters,
            page.Documents,
            page.SelectedCourseId,
            page.SelectedChapterId,
            permissions = new { canManage = isAdmin || isTeacher }
        });
    }

    [HttpGet("documents/{id:guid}")]
    public async Task<IActionResult> Document(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _documentService.GetDetailsAsync(id, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { error = "Document was not found." });
        }
    }

    [HttpGet("benchmark")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Benchmark(Guid? courseId, CancellationToken cancellationToken)
    {
        return Ok(await _benchmarkService.GetDashboardAsync(courseId, cancellationToken));
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(string? searchTerm, string? role, CancellationToken cancellationToken)
    {
        return Ok(new
        {
            users = await _userAdminService.ListAsync(searchTerm, role, cancellationToken),
            roles = new[] { "Student", "Teacher", "Admin" }
        });
    }
}
