using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Data;

public static class DatabaseBootstrapper
{
    private const string DefaultChunkingStrategy = "paragraph";
    private const string ChunkingStrategyKey = "ChunkingStrategy";
    private const string FixedChunkSizeKey = "FixedChunkSize";
    private const string FixedChunkOverlapKey = "FixedChunkOverlap";
    private const int DefaultFixedChunkSize = 1000;
    private const int DefaultFixedChunkOverlap = 150;

    public static async Task InitializeAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword)
    {
        await db.Database.MigrateAsync();
        await SeedSettingsAsync(db);
        await CreateBootstrapAdminAsync(db, configuration, hashPassword);
    }

    private static async Task CreateBootstrapAdminAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var email = RequireBootstrapValue(configuration, "Email").Trim().ToLowerInvariant();
        var displayName = RequireBootstrapValue(configuration, "FullName").Trim();
        var password = RequireBootstrapValue(configuration, "Password");
        var now = DateTime.UtcNow;
        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = UserRoleNames.Admin,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        admin.PasswordHash = hashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    private static string RequireBootstrapValue(IConfiguration configuration, string property)
    {
        var configurationKey = $"BootstrapAdmin:{property}";
        var value = configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing required bootstrap setting '{configurationKey}' for an empty database.");
        }

        return value;
    }

    private static async Task SeedSettingsAsync(AppDbContext db)
    {
        var keys = new[] { ChunkingStrategyKey, FixedChunkSizeKey, FixedChunkOverlapKey };
        var settings = await db.SystemSettings
            .Where(x => keys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key);
        var now = DateTime.UtcNow;
        var changed = false;
        if (!settings.ContainsKey(ChunkingStrategyKey))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = ChunkingStrategyKey,
                Value = DefaultChunkingStrategy,
                UpdatedAtUtc = now
            });
            changed = true;
        }

        if (!settings.ContainsKey(FixedChunkSizeKey))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = FixedChunkSizeKey,
                Value = DefaultFixedChunkSize.ToString(),
                UpdatedAtUtc = now
            });
            changed = true;
        }

        if (!settings.ContainsKey(FixedChunkOverlapKey))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = FixedChunkOverlapKey,
                Value = DefaultFixedChunkOverlap.ToString(),
                UpdatedAtUtc = now
            });
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
