using Microsoft.AspNetCore.Mvc;

namespace Prn222.UiLab.Controllers;

public sealed class WorkshopController : Controller
{
    public IActionResult Tokens() => View();
}
