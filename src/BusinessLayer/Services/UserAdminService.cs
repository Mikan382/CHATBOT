using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

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

    public async Task<IReadOnlyList<UserListDto>> ListAsync(
        string? searchTerm,
        string? role,
        CancellationToken cancellationToken)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role) ? null : role;
        var users = await _userRepository.ListUsersAsync(searchTerm, normalizedRole, cancellationToken);
        return users.Select(u => new UserListDto(u.Id, u.Email, u.DisplayName, u.Role, u.IsLockedOut)).ToList();
    }

    public async Task CreateAsync(
        string email,
        string fullName,
        string role,
        string password,
        CancellationToken cancellationToken)
    {
        role = ValidateRole(role);
        email = NormalizeEmail(email);
        var displayName = StringHelper.NormalizeRequired(fullName, "Display name");
        var passwordError = ValidatePassword(password);
        if (passwordError is not null)
        {
            throw new InvalidOperationException(passwordError);
        }

        if (await _userRepository.EmailExistsAsync(email, null, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = role,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Guid actorUserId,
        Guid userId,
        string email,
        string fullName,
        string role,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        role = ValidateRole(role);
        email = NormalizeEmail(email);
        var displayName = StringHelper.NormalizeRequired(fullName, "Display name");

        if (userId == actorUserId && role != UserRoleNames.Admin)
        {
            throw new InvalidOperationException("You cannot remove your own Admin role.");
        }

        await EnsureActiveAdminRemainsAsync(user, role != UserRoleNames.Admin, cancellationToken);
        if (await _userRepository.EmailExistsAsync(email, userId, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var changed = user.Email != email || user.DisplayName != displayName || user.Role != role;
        if (!changed)
        {
            return false;
        }

        var removeTeachingAssignments = user.Role == UserRoleNames.Teacher && role != UserRoleNames.Teacher;
        user.Email = email;
        user.DisplayName = displayName;
        user.Role = role;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveUserAsync(user, removeTeachingAssignments, cancellationToken);
        return userId == actorUserId;
    }

    public async Task SetLockoutAsync(
        Guid actorUserId,
        Guid userId,
        bool locked,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        if (locked && userId == actorUserId)
        {
            throw new InvalidOperationException("You cannot lock your own account.");
        }

        await EnsureActiveAdminRemainsAsync(user, locked, cancellationToken);
        if (user.IsLockedOut == locked)
        {
            return;
        }

        user.IsLockedOut = locked;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken)
    {
        if (userId == actorUserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        await EnsureActiveAdminRemainsAsync(user, true, cancellationToken);
        await _userRepository.DeleteAsync(user, cancellationToken);
    }

    public async Task ResetPasswordAsync(
        Guid actorUserId,
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken)
    {
        if (userId == actorUserId)
        {
            throw new InvalidOperationException("Use Change Password for your own account.");
        }

        var passwordError = ValidatePassword(newPassword);
        if (passwordError is not null)
        {
            throw new InvalidOperationException(passwordError);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    public static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return "Password is required.";
        if (password.Length < 8) return "Password must be at least 8 characters.";
        if (!password.Any(char.IsDigit)) return "Password must contain at least one digit.";
        if (!password.Any(char.IsLower)) return "Password must contain at least one lowercase letter.";
        if (!password.Any(char.IsUpper)) return "Password must contain at least one uppercase letter.";
        return null;
    }

    private async Task EnsureActiveAdminRemainsAsync(
        ApplicationUser user,
        bool removesActiveAdmin,
        CancellationToken cancellationToken)
    {
        if (removesActiveAdmin
            && user.Role == UserRoleNames.Admin
            && !user.IsLockedOut
            && await _userRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
        {
            throw new InvalidOperationException("At least one active Admin account is required.");
        }
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
        var normalized = StringHelper.NormalizeRequired(email, "Email").ToLowerInvariant();
        if (!MailAddress.TryCreate(normalized, out _))
        {
            throw new InvalidOperationException("Email is invalid.");
        }

        return normalized;
    }
}
