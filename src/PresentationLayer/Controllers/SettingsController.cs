using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : BaseController
{
    private readonly IChunkingSettingsService _chunkingSettingsService;

    public SettingsController(IChunkingSettingsService chunkingSettingsService)
    {
        _chunkingSettingsService = chunkingSettingsService;
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Index()
    {
        return RedirectToActionPermanent("Index", "Courses");
    }
}
