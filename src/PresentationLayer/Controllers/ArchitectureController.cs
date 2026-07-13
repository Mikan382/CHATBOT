using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class ArchitectureController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
