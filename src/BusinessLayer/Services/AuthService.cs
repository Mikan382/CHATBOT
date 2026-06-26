using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services;

public class AuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthService(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task SignInAsync(string email, string password, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Email and password are required.");
        }

        var result = await _signInManager.PasswordSignInAsync(email.Trim(), password, rememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return (false, "User not found.");
        }

        var result = await _signInManager.UserManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }

        return (true, null);
    }
}
