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

    public IActionResult Documents(string role = "teacher", string state = "ready")
    {
        ViewData["Role"] = role;
        ViewData["State"] = state;
        return View();
    }

    public IActionResult DocumentDetails(string state = "ready")
    {
        ViewData["State"] = state;
        return View();
    }
}
