using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.Helpers;

namespace BusinessLayer.Services;

public class UserAdminService : IUserAdminService
{
    private static readonly string[] AllowedRoles = [UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin];
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAdminRepository _userAdminRepository;

    public UserAdminService(UserManager<ApplicationUser> userManager, IUserAdminRepository userAdminRepository)
    {
        _userManager = userManager;
        _userAdminRepository = userAdminRepository;
    }

    public async Task<IReadOnlyList<UserListDto>> ListAsync()
    {
        var users = await _userAdminRepository.ListUsersWithRolesAsync();
        return users.Select(u => new UserListDto(u.Id, u.Email, u.FullName, u.Role, u.IsLockedOut)).ToList();
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
