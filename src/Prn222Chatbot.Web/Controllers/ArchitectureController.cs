using Microsoft.AspNetCore.Mvc;

namespace Prn222Chatbot.Web.Controllers;

public class ArchitectureController : Controller
{
    [HttpGet("/architecture")]
    public IActionResult Index()
    {
        return View();
    }
}
