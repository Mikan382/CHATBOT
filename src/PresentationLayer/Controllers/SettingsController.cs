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
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _chunkingSettingsService.GetAsync(cancellationToken);
        return View(new ChunkingSettingsViewModel
        {
            CurrentStrategy = settings.CurrentStrategy,
            AvailableStrategies = settings.AvailableStrategies
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ChunkingSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableStrategies = _chunkingSettingsService.AvailableStrategies;
            return View(model);
        }

        try
        {
            await _chunkingSettingsService.UpdateAsync(model.CurrentStrategy, cancellationToken);
            SetFlashSuccess("Chunking strategy was updated.");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            model.Error = UserFacingError(ex);
            model.AvailableStrategies = _chunkingSettingsService.AvailableStrategies;
            return View(model);
        }
    }
}
