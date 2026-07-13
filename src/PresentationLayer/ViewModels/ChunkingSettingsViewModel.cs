using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels;

public class ChunkingSettingsViewModel
{
    [Required]
    public string CurrentStrategy { get; set; } = "paragraph";
    public IReadOnlyList<string> AvailableStrategies { get; set; } = [];
    public string? Error { get; set; }
}
