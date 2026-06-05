using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataAccessLayer.Enums;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class ArchitectureController : Controller
{
    [HttpGet("/architecture")]
    public IActionResult Index()
    {
        return View();
    }
}
