using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;
using BusinessLayer.Helpers;

namespace BusinessLayer.Services;

public class UserAdminService : IUserAdminService
{
    private static readonly string[] AllowedRoles = [UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin];
    private readonly IUserAdminRepository _userRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public UserAdminService(IUserAdminRepository userRepository, IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserListDto>> ListAsync()
    {
        var users = await _userRepository.ListUsersAsync();
        return users.Select(u => new UserListDto(u.Id, u.Email, u.DisplayName, u.Role, u.IsLockedOut)).ToList();
    }

    public async Task CreateAsync(string email, string fullName, string role, string password)
    {
        role = ValidateRole(role);
        email = NormalizeEmail(email);
        var displayName = fullName?.Trim() ?? "";
        var passwordError = ValidatePassword(password);
        if (passwordError is not null)
        {
            throw new InvalidOperationException(passwordError);
        }

        if (await _userRepository.EmailExistsAsync(email, null))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = role,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        await _userRepository.AddAsync(user);
    }

    public async Task ChangeRoleAsync(Guid userId, string role)
    {
        role = ValidateRole(role);
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User was not found.");

        user.Role = role;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task SetLockoutAsync(Guid userId, bool locked)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User was not found.");

        user.IsLockedOut = locked;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User was not found.");

        await _userRepository.DeleteAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword)
    {
        var passwordError = ValidatePassword(newPassword);
        if (passwordError is not null)
        {
            throw new InvalidOperationException(passwordError);
        }

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User was not found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters.";
        }

        if (!password.Any(char.IsDigit))
        {
            return "Password must contain at least one digit.";
        }

        if (!password.Any(char.IsLower))
        {
            return "Password must contain at least one lowercase letter.";
        }

        if (!password.Any(char.IsUpper))
        {
            return "Password must contain at least one uppercase letter.";
        }

        return null;
    }

    private static string ValidateRole(string role)
    {
        if (!AllowedRoles.Contains(role))
        {
            throw new InvalidOperationException("Invalid role.");
        }

        return role;
    }

    private static string NormalizeEmail(string email)
    {
        return StringHelper.NormalizeRequired(email, "Email").ToLowerInvariant();
    }
}
