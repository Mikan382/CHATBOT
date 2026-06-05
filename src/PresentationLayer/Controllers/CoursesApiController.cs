using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLayer.Services;
using DataAccessLayer.Enums;

namespace PresentationLayer.Controllers;

[ApiController]
[Authorize(Roles = UserRoleNames.All)]
public class CoursesApiController : ControllerBase
{
    private readonly CourseService _courseService;

    public CoursesApiController(CourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet("/api/courses/current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCurrentAsync(cancellationToken);
        return Ok(new { success = true, course });
    }

    [HttpGet("/api/courses")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var courses = await _courseService.ListDtosAsync(cancellationToken);
        return Ok(new { success = true, courses });
    }

    [HttpGet("/api/courses/{id:guid}/chapters")]
    public async Task<IActionResult> Chapters(Guid id, CancellationToken cancellationToken)
    {
        var chapters = await _courseService.ListChaptersAsync(id, cancellationToken);
        return Ok(new { success = true, chapters });
    }
}
