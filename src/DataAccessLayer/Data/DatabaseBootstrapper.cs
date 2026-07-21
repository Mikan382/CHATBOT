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
    private const string DefaultPlanCode = "FREE";
    private const string DefaultPlanName = "Free";
    private const int DefaultPlanDurationDays = 30;
    private const long DefaultPlanTokenQuota = 3000;

    public static async Task InitializeAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword)
    {
        await db.Database.MigrateAsync();
        await EnsureSchemaUpdatesAsync(db);
        await SeedSettingsAsync(db);
        await SeedDefaultSubscriptionPlanAsync(db);
        await CreateBootstrapAdminAsync(db, configuration, hashPassword);
    }

    // Seeds a single active free package that new students are attached to.
    // Idempotent by emptiness: it only runs when no plan exists yet, so once an
    // admin has any package (or edits the free plan's quota), the seed is skipped
    // and their values are preserved. Deleting every plan lets it restore on next start.
    private static async Task SeedDefaultSubscriptionPlanAsync(AppDbContext db)
    {
        if (await db.SubscriptionPlans.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = DefaultPlanCode,
            Name = DefaultPlanName,
            Description = "Default free package for new students.",
            Price = 0m,
            DurationDays = DefaultPlanDurationDays,
            TokenQuota = DefaultPlanTokenQuota,
            SortOrder = 0,
            IsActive = true,
            IsDefault = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureSchemaUpdatesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CourseTeachers]') AND name = N'IsHead')
BEGIN
    ALTER TABLE [CourseTeachers] ADD [IsHead] bit NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Courses]') AND name = N'DefaultChunkingStrategy')
BEGIN
    ALTER TABLE [Courses] ADD [DefaultChunkingStrategy] nvarchar(64) NULL;
    ALTER TABLE [Courses] ADD [DefaultChunkSize] int NULL;
    ALTER TABLE [Courses] ADD [DefaultChunkOverlap] int NULL;
    ALTER TABLE [Courses] ADD [DefaultEmbeddingModel] nvarchar(160) NULL;
END

");
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
