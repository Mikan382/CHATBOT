using BusinessLayer.DTOs;
using BusinessLayer.Indexing;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class ChunkingSettingsService : IChunkingSettingsService
{
    private const string StrategySettingKey = "ChunkingStrategy";
    private const string FixedChunkSizeSettingKey = "FixedChunkSize";
    private const string FixedChunkOverlapSettingKey = "FixedChunkOverlap";

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
        var values = await _settingsRepository.GetValuesAsync(
            [StrategySettingKey, FixedChunkSizeSettingKey, FixedChunkOverlapSettingKey],
            cancellationToken);
        if (!values.TryGetValue(StrategySettingKey, out var storedStrategy)
            || !values.TryGetValue(FixedChunkSizeSettingKey, out var storedChunkSize)
            || !values.TryGetValue(FixedChunkOverlapSettingKey, out var storedOverlap))
        {
            throw new InvalidOperationException("Chunking settings are incomplete.");
        }

        if (!int.TryParse(storedChunkSize, out var chunkSize)
            || !int.TryParse(storedOverlap, out var overlap)
            || !IsValidFixedConfiguration(chunkSize, overlap))
        {
            throw new InvalidOperationException("Stored fixed-size chunking settings are invalid.");
        }

        var strategy = NormalizeStrategy(storedStrategy);
        if (!IsAllowed(strategy))
        {
            throw new InvalidOperationException("Stored chunking strategy is invalid.");
        }

        return new ChunkingSettingsDto(
            strategy,
            chunkSize,
            overlap,
            _availableStrategies);
    }

    public async Task UpdateAsync(
        string strategyName,
        int fixedChunkSize,
        int fixedChunkOverlap,
        CancellationToken cancellationToken)
    {
        var normalizedStrategy = NormalizeStrategy(strategyName);
        if (!IsAllowed(normalizedStrategy))
        {
            throw new InvalidOperationException("Invalid chunking strategy.");
        }

        if (!IsValidFixedConfiguration(fixedChunkSize, fixedChunkOverlap))
        {
            throw new InvalidOperationException(
                $"Fixed chunk size must be {FixedSizeChunker.MinChunkSize}-{FixedSizeChunker.MaxChunkSize} characters, "
                + $"and overlap must be 0-{FixedSizeChunker.MaxOverlap} characters and smaller than chunk size.");
        }

        await _settingsRepository.SetValuesAsync(new Dictionary<string, string>
        {
            [StrategySettingKey] = normalizedStrategy,
            [FixedChunkSizeSettingKey] = fixedChunkSize.ToString(),
            [FixedChunkOverlapSettingKey] = fixedChunkOverlap.ToString()
        }, cancellationToken);
    }

    private bool IsAllowed(string? strategyName)
    {
        return !string.IsNullOrWhiteSpace(strategyName)
            && _availableStrategies.Contains(strategyName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidFixedConfiguration(int chunkSize, int overlap)
    {
        return chunkSize is >= FixedSizeChunker.MinChunkSize and <= FixedSizeChunker.MaxChunkSize
            && overlap is >= 0 and <= FixedSizeChunker.MaxOverlap
            && overlap < chunkSize;
    }

    private static string NormalizeStrategy(string? strategyName)
    {
        return strategyName?.Trim().ToLowerInvariant() ?? "";
    }
}
