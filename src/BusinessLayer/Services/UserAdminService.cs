using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services;

public class UserAdminService
{
    private static readonly string[] AllowedRoles = [UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin];
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserListDto>> ListAsync()
    {
        var users = _userManager.Users
            .OrderBy(x => x.Email)
            .ToList();

        var rows = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new UserListDto(
                user.Id,
                user.Email ?? "",
                user.FullName,
                roles.FirstOrDefault() ?? "",
                user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow));
        }

        return rows;
    }

    public async Task CreateAsync(string email, string fullName, string role, string password)
    {
        role = ValidateRole(role);
        email = NormalizeRequired(email, "Email");
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

    private static string ValidateRole(string role)
    {
        if (!AllowedRoles.Contains(role))
        {
            throw new InvalidOperationException("Invalid role.");
        }

        return role;
    }

    private static string NormalizeRequired(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
        }
    }
}
