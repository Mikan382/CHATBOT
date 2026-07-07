namespace DataAccessLayer.Repositories;

public interface ISystemSettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken);
}
