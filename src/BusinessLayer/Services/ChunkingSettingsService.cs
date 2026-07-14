using BusinessLayer.DTOs;
using BusinessLayer.Indexing;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class ChunkingSettingsService : IChunkingSettingsService
{
    public const string StrategySettingKey = "ChunkingStrategy";
    public const string FixedChunkSizeSettingKey = "FixedChunkSize";
    public const string FixedChunkOverlapSettingKey = "FixedChunkOverlap";
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
        var values = await _settingsRepository.GetValuesAsync(
            [StrategySettingKey, FixedChunkSizeSettingKey, FixedChunkOverlapSettingKey],
            cancellationToken);
        values.TryGetValue(StrategySettingKey, out var storedStrategy);
        values.TryGetValue(FixedChunkSizeSettingKey, out var storedChunkSize);
        values.TryGetValue(FixedChunkOverlapSettingKey, out var storedOverlap);

        var chunkSize = ParseSetting(storedChunkSize, FixedSizeChunker.DefaultChunkSize);
        if (TryParseLegacyFixedSize(storedStrategy, out var legacyChunkSize)
            && string.IsNullOrWhiteSpace(storedChunkSize))
        {
            chunkSize = legacyChunkSize;
        }

        var overlap = ParseSetting(storedOverlap, FixedSizeChunker.DefaultOverlap);
        if (!IsValidFixedConfiguration(chunkSize, overlap))
        {
            chunkSize = FixedSizeChunker.DefaultChunkSize;
            overlap = FixedSizeChunker.DefaultOverlap;
        }

        var strategy = NormalizeStrategy(storedStrategy);
        return new ChunkingSettingsDto(
            IsAllowed(strategy) ? strategy : DefaultStrategy,
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

    private static int ParseSetting(string? value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string NormalizeStrategy(string? strategyName)
    {
        var normalized = strategyName?.Trim().ToLowerInvariant() ?? "";
        return normalized.StartsWith("fixed_", StringComparison.Ordinal) ? "fixed" : normalized;
    }

    private static bool TryParseLegacyFixedSize(string? strategyName, out int chunkSize)
    {
        chunkSize = 0;
        if (string.IsNullOrWhiteSpace(strategyName)
            || !strategyName.StartsWith("fixed_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(strategyName["fixed_".Length..], out chunkSize);
    }
}
