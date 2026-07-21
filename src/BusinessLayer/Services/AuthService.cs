using Microsoft.AspNetCore.Identity;
using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using DataAccessLayer.Repositories;

namespace BusinessLayer.Services;

public class AuthService : IAuthService
{
    private readonly IUserAdminRepository _userRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IUserAdminService _userAdminService;

    public AuthService(
        IUserAdminRepository userRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IUserAdminService userAdminService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _userAdminService = userAdminService;
    }

    public async Task<AuthenticatedUserDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Email and password are required.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (user.IsLockedOut)
        {
            throw new InvalidOperationException("This account has been locked. Please contact an administrator.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        return ToDto(user);
    }

    public async Task<bool> IsPrincipalCurrentAsync(
        Guid userId,
        string email,
        string role,
        long userVersion,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is not null
            && !user.IsLockedOut
            && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
            && user.Role == role
            && user.UpdatedAtUtc.Ticks == userVersion;
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return (false, "User not found.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            return (false, "Current password is incorrect.");
        }

        var passwordError = UserAdminService.ValidatePassword(newPassword);
        if (passwordError is not null)
        {
            return (false, passwordError);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<AuthenticatedUserDto> RegisterAsync(
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var userId = await _userAdminService.CreateAsync(
            email,
            fullName,
            UserRoleNames.Student,
            password,
            cancellationToken);

        var createdUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load registered user.");

        return ToDto(createdUser);
    }

    private static AuthenticatedUserDto ToDto(ApplicationUser user)
    {
        return new AuthenticatedUserDto(user.Id, user.Email, user.Role, user.UpdatedAtUtc);
    }
}
