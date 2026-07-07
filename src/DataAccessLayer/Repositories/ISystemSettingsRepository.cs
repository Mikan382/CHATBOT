using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface ISystemSettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken);
}

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly AppDbContext _db;

    public SystemSettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        return await _db.SystemSettings
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting is null)
        {
            _db.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
