using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data.Seed;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Data;

public static class DatabaseBootstrapper
{
    private const string DefaultChunkingStrategy = "paragraph";

    public static async Task InitializeAsync(
        IServiceProvider services,
        Func<ApplicationUser, string, string> hashPassword,
        Func<ApplicationUser, string, bool> verifyPassword)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();
        await Prn222SeedData.SeedAsync(db);
        var teacher = await SeedUsersAsync(db, configuration, hashPassword, verifyPassword);
        await SeedTeacherAssignmentAsync(db, teacher.Id);
        await SeedSettingsAsync(db);
    }

    private static async Task<ApplicationUser> SeedUsersAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword,
        Func<ApplicationUser, string, bool> verifyPassword)
    {
        var student = await SeedUserAsync(db, configuration, hashPassword, verifyPassword, "Student", "student@prn222.local", "Student Demo", UserRoleNames.Student);
        var teacher = await SeedUserAsync(db, configuration, hashPassword, verifyPassword, "Teacher", "teacher@prn222.local", "Teacher Demo", UserRoleNames.Teacher);
        var admin = await SeedUserAsync(db, configuration, hashPassword, verifyPassword, "Admin", "admin@prn222.local", "Admin Demo", UserRoleNames.Admin);

        _ = student;
        _ = admin;
        await db.SaveChangesAsync();
        return teacher;
    }

    private static async Task<ApplicationUser> SeedUserAsync(
        AppDbContext db,
        IConfiguration configuration,
        Func<ApplicationUser, string, string> hashPassword,
        Func<ApplicationUser, string, bool> verifyPassword,
        string key,
        string defaultEmail,
        string defaultDisplayName,
        string role)
    {
        var email = (configuration[$"SeedUsers:{key}:Email"] ?? defaultEmail).Trim().ToLowerInvariant();
        var displayName = configuration[$"SeedUsers:{key}:FullName"] ?? defaultDisplayName;
        var password = configuration[$"SeedUsers:{key}:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Prn222@123";
            Console.WriteLine($"[SEED] No password configured for '{key}'. Using default dev password for {defaultEmail}.");
        }

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = displayName,
                Role = role,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            user.PasswordHash = hashPassword(user, password);
            db.Users.Add(user);
            return user;
        }

        user.DisplayName = displayName;
        user.Role = role;
        user.UpdatedAtUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !verifyPassword(user, password))
        {
            user.PasswordHash = hashPassword(user, password);
        }

        return user;
    }

    private static async Task SeedTeacherAssignmentAsync(AppDbContext db, Guid teacherId)
    {
        var exists = await db.CourseTeachers.AnyAsync(
            x => x.CourseId == Prn222SeedData.CourseId && x.TeacherUserId == teacherId);
        if (!exists)
        {
            db.CourseTeachers.Add(new CourseTeacher
            {
                CourseId = Prn222SeedData.CourseId,
                TeacherUserId = teacherId,
                AssignedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedSettingsAsync(AppDbContext db)
    {
        var exists = await db.SystemSettings.AnyAsync(x => x.Key == "ChunkingStrategy");
        if (!exists)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = "ChunkingStrategy",
                Value = DefaultChunkingStrategy,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }
}
