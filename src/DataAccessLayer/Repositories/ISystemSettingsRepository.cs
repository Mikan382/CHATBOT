namespace DataAccessLayer.Repositories;

public interface ISystemSettingsRepository
{
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken);
    Task SetValuesAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken);
}
