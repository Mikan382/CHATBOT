using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

public class ArchitectureController : Controller
{
    [HttpGet("/architecture")]
    public IActionResult Index()
    {
        return View();
    }
}
