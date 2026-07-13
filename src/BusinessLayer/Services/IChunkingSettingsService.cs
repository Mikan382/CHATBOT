using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IChunkingSettingsService
{
    IReadOnlyList<string> AvailableStrategies { get; }
    Task<ChunkingSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<string> GetCurrentStrategyAsync(CancellationToken cancellationToken);
    Task UpdateAsync(string strategyName, CancellationToken cancellationToken);
}
