using System.ComponentModel.DataAnnotations;
using BusinessLayer.Indexing;

namespace PresentationLayer.ViewModels;

public class ChunkingSettingsViewModel
{
    [Required]
    public string CurrentStrategy { get; set; } = "paragraph";

    [Range(FixedSizeChunker.MinChunkSize, FixedSizeChunker.MaxChunkSize)]
    public int FixedChunkSize { get; set; } = FixedSizeChunker.DefaultChunkSize;

    [Range(0, FixedSizeChunker.MaxOverlap)]
    public int FixedChunkOverlap { get; set; } = FixedSizeChunker.DefaultOverlap;

    public IReadOnlyList<string> AvailableStrategies { get; set; } = [];
    public string? Error { get; set; }
}
