using Microsoft.AspNetCore.Mvc;
using Prn222Chatbot.Web.Services;

namespace Prn222Chatbot.Web.Controllers;

[ApiController]
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
}
