using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DataAccessLayer.Data.Seed;
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
    private const string DemoSeedVersionKey = "DemoSeedVersion";
    private const string DemoSeedVersion = "1";
    private static readonly Guid FreePlanId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid StandardPlanId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid PremiumPlanId = Guid.Parse("20000000-0000-0000-0000-000000000003");

    public static async Task InitializeAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword)
    {
        await db.Database.MigrateAsync();
        await SeedSettingsAsync(db);
        await SeedDemoDataOnceAsync(db, configuration, hashPassword);
    }

    private static async Task SeedDemoDataOnceAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword)
    {
        if (await db.SystemSettings.AnyAsync(x => x.Key == DemoSeedVersionKey))
        {
            return;
        }

        var hasExistingData = await db.Users.AnyAsync()
            || await db.Courses.AnyAsync()
            || await db.SubscriptionPlans.AnyAsync()
            || await db.StudentSubscriptions.AnyAsync()
            || await db.Documents.AnyAsync()
            || await db.ChatSessions.AnyAsync();

        if (!hasExistingData)
        {
            AddDemoData(db, configuration, hashPassword);
        }

        db.SystemSettings.Add(new SystemSetting
        {
            Key = DemoSeedVersionKey,
            Value = DemoSeedVersion,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static void AddDemoData(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword)
    {
        var now = DateTime.UtcNow;
        Prn222SeedData.AddTo(db);

        var student = CreateSeedUser(configuration, hashPassword, "Student", UserRoleNames.Student, now);
        var teacher = CreateSeedUser(configuration, hashPassword, "Teacher", UserRoleNames.Teacher, now);
        var admin = CreateSeedUser(configuration, hashPassword, "Admin", UserRoleNames.Admin, now);
        db.Users.AddRange(student, teacher, admin);

        db.CourseTeachers.Add(new CourseTeacher
        {
            CourseId = Prn222SeedData.CourseId,
            TeacherUserId = teacher.Id,
            AssignedAtUtc = now
        });

        var subscriptionPlans = CreateSubscriptionPlans(now);
        db.SubscriptionPlans.AddRange(subscriptionPlans);
        var freePlan = subscriptionPlans.Single(x => x.Id == FreePlanId);
        db.StudentSubscriptions.Add(new StudentSubscription
        {
            Id = Guid.NewGuid(),
            StudentUserId = student.Id,
            SubscriptionPlanId = FreePlanId,
            Status = SubscriptionStatusNames.Active,
            PriceAtActivation = 0,
            MessageQuotaAtActivation = freePlan.MessageQuota,
            StartedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private static ApplicationUser CreateSeedUser(
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword,
        string key,
        string role,
        DateTime now)
    {
        var email = RequireSeedValue(configuration, key, "Email").Trim().ToLowerInvariant();
        var displayName = RequireSeedValue(configuration, key, "FullName").Trim();
        var password = RequireSeedValue(configuration, key, "Password");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = role,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.PasswordHash = hashPassword(user, password);
        return user;
    }

    private static string RequireSeedValue(IConfiguration configuration, string userKey, string property)
    {
        var configurationKey = $"SeedUsers:{userKey}:{property}";
        var value = configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required demo seed setting '{configurationKey}'.");
        }

        return value;
    }

    private static IReadOnlyList<SubscriptionPlan> CreateSubscriptionPlans(DateTime now)
    {
        return
        [
            new SubscriptionPlan
            {
                Id = FreePlanId,
                Code = "FREE",
                Name = "Free",
                Description = "Internal demo access for basic course chat and document lookup.",
                MonthlyPrice = 0,
                DurationDays = 30,
                MessageQuota = 20,
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SubscriptionPlan
            {
                Id = StandardPlanId,
                Code = "STANDARD",
                Name = "Standard",
                Description = "Demo package for regular students who use course support during the semester.",
                MonthlyPrice = 49000,
                DurationDays = 30,
                MessageQuota = 200,
                SortOrder = 2,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SubscriptionPlan
            {
                Id = PremiumPlanId,
                Code = "PREMIUM",
                Name = "Premium",
                Description = "Demo package for students who need extended AI course assistance.",
                MonthlyPrice = 99000,
                DurationDays = 30,
                MessageQuota = 0,
                SortOrder = 3,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        ];
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
