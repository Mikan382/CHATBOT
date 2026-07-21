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
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public UserAdminService(
        IUserAdminRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
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

    public async Task<Guid> CreateAsync(
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
        var defaultSubscription = role == UserRoleNames.Student
            ? await CreateDefaultSubscriptionAsync(user.Id, now, cancellationToken)
            : null;
        await _userRepository.AddAsync(user, defaultSubscription, cancellationToken);
        return user.Id;
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
        var previousRole = user.Role;
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

        var removeTeachingAssignments = previousRole == UserRoleNames.Teacher && role != UserRoleNames.Teacher;
        var expireStudentSubscriptions = previousRole == UserRoleNames.Student && role != UserRoleNames.Student;
        StudentSubscription? defaultSubscription = null;
        if (previousRole != UserRoleNames.Student && role == UserRoleNames.Student)
        {
            var current = await _subscriptionRepository.GetCurrentForStudentAsync(
                userId,
                DateTime.UtcNow,
                cancellationToken);
            if (current is null)
            {
                defaultSubscription = await CreateDefaultSubscriptionAsync(
                    userId,
                    DateTime.UtcNow,
                    cancellationToken);
            }
        }

        user.Email = email;
        user.DisplayName = displayName;
        user.Role = role;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveUserAsync(
            user,
            removeTeachingAssignments,
            expireStudentSubscriptions,
            defaultSubscription,
            cancellationToken);
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
        if (await _userRepository.HasRelatedDataAsync(userId, cancellationToken))
        {
            throw new InvalidOperationException(
                "This user has related application data. Lock the account instead of deleting it.");
        }

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

    private async Task<StudentSubscription> CreateDefaultSubscriptionAsync(
        Guid studentUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var plan = await _subscriptionRepository.GetDefaultPlanAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Configure an active free default package before creating a Student account.");

        return new StudentSubscription
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatusNames.Active,
            PriceAtActivation = plan.Price,
            TokenQuotaAtActivation = plan.TokenQuota,
            StartedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddDays(plan.DurationDays),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public async Task<BatchImportResultDto> ImportStudentsFromCsvAsync(
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var totalRows = 0;
        var successCount = 0;
        var skippedCount = 0;
        const string defaultPassword = "Student@123";

        using var reader = new System.IO.StreamReader(fileStream, System.Text.Encoding.UTF8);
        string? headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine is null)
        {
            return new BatchImportResultDto(0, 0, 0, new[] { "The uploaded CSV file is empty." });
        }

        var nameIndex = 0;
        var emailIndex = 1;
        var headers = headerLine.Split(',', ';');
        if (headers.Length >= 2)
        {
            var h0 = headers[0].Trim().ToLowerInvariant();
            var h1 = headers[1].Trim().ToLowerInvariant();
            if (h0.Contains("email") || h0.Contains("mail"))
            {
                emailIndex = 0;
                nameIndex = 1;
            }
            else if (h1.Contains("name") || h1.Contains("fullname") || h1.Contains("ten") || h1.Contains("ho"))
            {
                nameIndex = 0;
                emailIndex = 1;
            }
        }

        string? line;
        var lineNumber = 1;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',', ';');
            if (parts.Length < 2)
            {
                errors.Add($"Line {lineNumber}: Invalid format. Expected 'FullName, Email'.");
                continue;
            }

            var colA = parts[0].Trim();
            var colB = parts[1].Trim();
            string rawName, rawEmail;

            if (colA.Contains('@') && !colB.Contains('@'))
            {
                rawEmail = colA;
                rawName = colB;
            }
            else if (colB.Contains('@') && !colA.Contains('@'))
            {
                rawEmail = colB;
                rawName = colA;
            }
            else
            {
                rawName = nameIndex < parts.Length ? parts[nameIndex].Trim() : parts[0].Trim();
                rawEmail = emailIndex < parts.Length ? parts[emailIndex].Trim() : parts[1].Trim();
            }

            if (string.IsNullOrWhiteSpace(rawEmail) || !rawEmail.Contains('@'))
            {
                errors.Add($"Line {lineNumber}: Invalid email '{rawEmail}'.");
                continue;
            }

            totalRows++;
            string email;
            try
            {
                email = NormalizeEmail(rawEmail);
            }
            catch (Exception ex)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
                continue;
            }

            var fullName = string.IsNullOrWhiteSpace(rawName) ? email.Split('@')[0] : rawName;

            if (await _userRepository.EmailExistsAsync(email, null, cancellationToken))
            {
                skippedCount++;
                continue;
            }

            try
            {
                var now = DateTime.UtcNow;
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    DisplayName = fullName,
                    Role = UserRoleNames.Student,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, defaultPassword);
                var subscription = await CreateDefaultSubscriptionAsync(user.Id, now, cancellationToken);
                await _userRepository.AddAsync(user, subscription, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Line {lineNumber} ({email}): {ex.Message}");
            }
        }

        return new BatchImportResultDto(totalRows, successCount, skippedCount, errors);
    }
}
