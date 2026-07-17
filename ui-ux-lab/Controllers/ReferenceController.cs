using Microsoft.AspNetCore.Mvc;

namespace Prn222.UiLab.Controllers;

public sealed class ReferenceController : Controller
{
    public IActionResult Shell() => View();

    public IActionResult Chat(string state = "active")
    {
        ViewData["State"] = state;
        return View();
    }

    public IActionResult Login(string state = "default")
    {
        ViewData["State"] = state;
        return View();
    }
}
