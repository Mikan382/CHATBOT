using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Data.Seed;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Data;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await Prn222SeedData.SeedAsync(db);

        await NormalizeDocumentProgressAsync(db);
        await SeedIdentityAsync(scope.ServiceProvider);
    }

    private static async Task NormalizeDocumentProgressAsync(AppDbContext db)
    {
        foreach (var document in db.Documents)
        {
            switch (document.IndexStatus)
            {
                case DocumentIndexStatus.Indexed:
                    document.IndexProgressPercent = 100;
                    document.IndexStage = "Indexed";
                    break;
                case DocumentIndexStatus.Failed:
                    document.IndexStage = string.IsNullOrWhiteSpace(document.IndexStage) ? "Failed" : document.IndexStage;
                    break;
                case DocumentIndexStatus.Processing:
                    document.IndexProgressPercent = Math.Clamp(document.IndexProgressPercent, 10, 95);
                    document.IndexStage = string.IsNullOrWhiteSpace(document.IndexStage) ? "Processing" : document.IndexStage;
                    break;
                default:
                    document.IndexProgressPercent = 0;
                    document.IndexStage = "Queued";
                    break;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedIdentityAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedUserAsync(userManager, configuration, "Student", "student@prn222.local", "Student Demo", UserRoleNames.Student);
        await SeedUserAsync(userManager, configuration, "Teacher", "teacher@prn222.local", "Teacher Demo", UserRoleNames.Teacher);
        await SeedUserAsync(userManager, configuration, "Admin", "admin@prn222.local", "Admin Demo", UserRoleNames.Admin);
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string key,
        string defaultEmail,
        string defaultFullName,
        string role)
    {
        var email = configuration[$"SeedUsers:{key}:Email"] ?? defaultEmail;
        var fullName = configuration[$"SeedUsers:{key}:FullName"] ?? defaultFullName;
        var password = configuration[$"SeedUsers:{key}:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            // Fresh-install fallback: app is usable without user-secrets.
            // Override via SeedUsers:{key}:Password in production.
            password = "Prn222@123";
            Console.WriteLine($"[SEED] No password configured for '{key}'. Using default dev password for {defaultEmail}.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }
}
