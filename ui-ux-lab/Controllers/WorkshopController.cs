using Microsoft.AspNetCore.Mvc;

namespace Prn222.UiLab.Controllers;

public sealed class WorkshopController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Tokens() => View();

    public IActionResult Primitives() => View();

    public IActionResult Composites() => View();
}
