using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IChunkingSettingsService
{
    IReadOnlyList<string> AvailableStrategies { get; }
    Task<ChunkingSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task UpdateAsync(
        string strategyName,
        int fixedChunkSize,
        int fixedChunkOverlap,
        CancellationToken cancellationToken);
}
