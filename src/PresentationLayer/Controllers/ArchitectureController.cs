using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class ArchitectureController : BaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
