using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly AppDbContext _db;

    public SystemSettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken)
    {
        return await _db.SystemSettings
            .Where(x => keys.Contains(x.Key))
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
    }

    public async Task SetValuesAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var keys = values.Keys.ToList();
        var existing = await _db.SystemSettings
            .Where(x => keys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var item in values)
        {
            if (existing.TryGetValue(item.Key, out var setting))
            {
                setting.Value = item.Value;
                setting.UpdatedAtUtc = now;
            }
            else
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Key = item.Key,
                    Value = item.Value,
                    UpdatedAtUtc = now
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
