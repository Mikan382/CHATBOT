using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using BusinessLayer.Helpers;

namespace BusinessLayer.Services;

public class UserAdminService : IUserAdminService
{
    private static readonly string[] AllowedRoles = [UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin];
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public UserAdminService(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IReadOnlyList<UserListDto>> ListAsync()
    {
        // 2 queries instead of N+1: load users, then batch load role names
        var users = await _userManager.Users
            .OrderBy(x => x.Email)
            .AsNoTracking()
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Single join query for all user roles
        var userRoles = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }
        ).ToListAsync();

        var roleLookup = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        return users.Select(u => new UserListDto(
            u.Id,
            u.Email ?? "",
            u.FullName,
            roleLookup.TryGetValue(u.Id, out var roles) ? roles.FirstOrDefault() ?? "" : "",
            u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow))
        .ToList();
    }

    public async Task CreateAsync(string email, string fullName, string role, string password)
    {
        role = ValidateRole(role);
        email = StringHelper.NormalizeRequired(email, "Email");
        fullName = fullName?.Trim() ?? "";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };

        var result = await _userManager.CreateAsync(user, password);
        EnsureSuccess(result);
        result = await _userManager.AddToRoleAsync(user, role);
        EnsureSuccess(result);
    }

    public async Task ChangeRoleAsync(Guid userId, string role)
    {
        role = ValidateRole(role);
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User was not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            EnsureSuccess(await _userManager.RemoveFromRolesAsync(user, currentRoles));
        }

        EnsureSuccess(await _userManager.AddToRoleAsync(user, role));
    }

    public async Task SetLockoutAsync(Guid userId, bool locked)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User was not found.");

        EnsureSuccess(await _userManager.SetLockoutEnabledAsync(user, true));
        EnsureSuccess(await _userManager.SetLockoutEndDateAsync(
            user,
            locked ? DateTimeOffset.UtcNow.AddYears(100) : null));
    }

    public async Task DeleteAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User was not found.");

        EnsureSuccess(await _userManager.DeleteAsync(user));
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("New password is required.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User was not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSuccess(await _userManager.ResetPasswordAsync(user, token, newPassword));
    }

    private static string ValidateRole(string role)
    {
        if (!AllowedRoles.Contains(role))
        {
            throw new InvalidOperationException("Invalid role.");
        }

        return role;
    }


    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
        }
    }
}
