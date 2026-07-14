using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[Authorize]
public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Chat");
    }

    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
