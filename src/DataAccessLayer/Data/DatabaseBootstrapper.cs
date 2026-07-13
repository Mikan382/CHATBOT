using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DataAccessLayer.Data.Seed;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Data;

public static class DatabaseBootstrapper
{
    private const string DefaultChunkingStrategy = "paragraph";
    private const string DemoSeedVersionKey = "DemoSeedVersion";
    private const string DemoSeedVersion = "1";
    private const string DocumentHashVersionKey = "DocumentHashVersion";
    private const string DocumentHashVersion = "3";
    private static readonly Guid FreePlanId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid StandardPlanId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid PremiumPlanId = Guid.Parse("20000000-0000-0000-0000-000000000003");

    public static async Task InitializeAsync(
        IServiceProvider services,
        Func<ApplicationUser, string, string> hashPassword,
        Func<string, string> computeDocumentHash)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();
        await SeedSettingsAsync(db);
        await SeedDemoDataOnceAsync(db, configuration, hashPassword);
        await BackfillDocumentHashesAsync(db, computeDocumentHash);
    }

    private static async Task BackfillDocumentHashesAsync(
        AppDbContext db,
        Func<string, string> computeDocumentHash)
    {
        var version = await db.SystemSettings
            .FirstOrDefaultAsync(x => x.Key == DocumentHashVersionKey);
        if (version?.Value == DocumentHashVersion)
        {
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var documents = await db.Documents
            .OrderBy(x => x.UploadedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();

        foreach (var document in documents)
        {
            document.ContentHash = $"tmp{document.Id:N}";
        }
        await db.SaveChangesAsync();

        var seen = new HashSet<(Guid ChapterId, string Hash)>();
        var duplicateCount = 0;
        foreach (var document in documents)
        {
            var hash = computeDocumentHash(document.ContentText);
            if (!seen.Add((document.ChapterId, hash)))
            {
                db.Documents.Remove(document);
                duplicateCount++;
                continue;
            }

            document.ContentHash = hash;
        }

        if (version is null)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = DocumentHashVersionKey,
                Value = DocumentHashVersion,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            version.Value = DocumentHashVersion;
            version.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        if (duplicateCount > 0)
        {
            Console.WriteLine($"[DATA] Removed {duplicateCount} duplicate document(s) while backfilling content hashes.");
        }
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

        var student = CreateSeedUser(configuration, hashPassword, "Student", "student@prn222.local", "Student Demo", UserRoleNames.Student, now);
        var teacher = CreateSeedUser(configuration, hashPassword, "Teacher", "teacher@prn222.local", "Teacher Demo", UserRoleNames.Teacher, now);
        var admin = CreateSeedUser(configuration, hashPassword, "Admin", "admin@prn222.local", "Admin Demo", UserRoleNames.Admin, now);
        db.Users.AddRange(student, teacher, admin);

        db.CourseTeachers.Add(new CourseTeacher
        {
            CourseId = Prn222SeedData.CourseId,
            TeacherUserId = teacher.Id,
            AssignedAtUtc = now
        });

        db.SubscriptionPlans.AddRange(CreateSubscriptionPlans(now));
        db.StudentSubscriptions.Add(new StudentSubscription
        {
            Id = Guid.NewGuid(),
            StudentUserId = student.Id,
            SubscriptionPlanId = FreePlanId,
            Status = SubscriptionStatusNames.Active,
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
        string defaultEmail,
        string defaultDisplayName,
        string role,
        DateTime now)
    {
        var email = (configuration[$"SeedUsers:{key}:Email"] ?? defaultEmail).Trim().ToLowerInvariant();
        var displayName = configuration[$"SeedUsers:{key}:FullName"] ?? defaultDisplayName;
        var password = configuration[$"SeedUsers:{key}:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Prn222@123";
            Console.WriteLine($"[SEED] No password configured for '{key}'. Using the default dev password for {email}.");
        }

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
                SortOrder = 3,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        ];
    }

    private static async Task SeedSettingsAsync(AppDbContext db)
    {
        if (await db.SystemSettings.AnyAsync(x => x.Key == "ChunkingStrategy"))
        {
            return;
        }

        db.SystemSettings.Add(new SystemSetting
        {
            Key = "ChunkingStrategy",
            Value = DefaultChunkingStrategy,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
