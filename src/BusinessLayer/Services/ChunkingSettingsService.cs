using BusinessLayer.Indexing;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class ChunkingSettingsService : IChunkingSettingsService
{
    public const string SettingKey = "ChunkingStrategy";
    public const string DefaultStrategy = "paragraph";

    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IReadOnlyList<string> _availableStrategies;

    public ChunkingSettingsService(ISystemSettingsRepository settingsRepository, IEnumerable<ITextChunker> chunkers)
    {
        _settingsRepository = settingsRepository;
        _availableStrategies = chunkers
            .Select(x => x.StrategyName)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    public IReadOnlyList<string> AvailableStrategies => _availableStrategies;

    public async Task<ChunkingSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        return new ChunkingSettingsDto(await GetCurrentStrategyAsync(cancellationToken), _availableStrategies);
    }

    public async Task<string> GetCurrentStrategyAsync(CancellationToken cancellationToken)
    {
        var value = await _settingsRepository.GetValueAsync(SettingKey, cancellationToken);
        return IsAllowed(value) ? value! : DefaultStrategy;
    }

    public async Task UpdateAsync(string strategyName, CancellationToken cancellationToken)
    {
        if (!IsAllowed(strategyName))
        {
            throw new InvalidOperationException("Invalid chunking strategy.");
        }

        await _settingsRepository.SetValueAsync(SettingKey, strategyName, cancellationToken);
    }

    private bool IsAllowed(string? strategyName)
    {
        return !string.IsNullOrWhiteSpace(strategyName)
            && _availableStrategies.Contains(strategyName);
    }
}
