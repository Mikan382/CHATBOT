using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Repositories;

public class UserAdminRepository : IUserAdminRepository
{
    private readonly AppDbContext _db;

    public UserAdminRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserListRow>> ListUsersAsync(
        string? searchTerm,
        string? role,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x => x.Email.Contains(term) || x.DisplayName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.Role == role);
        }

        return await query
            .OrderBy(x => x.Email)
            .AsNoTracking()
            .Select(user => new UserListRow(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role,
                user.IsLockedOut))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationUser>> ListUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Where(x => x.Role == role && !x.IsLockedOut)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _db.Users.AnyAsync(
            x => x.Email == normalizedEmail && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task AddAsync(
        ApplicationUser user,
        StudentSubscription? defaultSubscription,
        CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        if (defaultSubscription is not null)
        {
            _db.StudentSubscriptions.Add(defaultSubscription);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveUserAsync(
        ApplicationUser user,
        bool removeTeachingAssignments,
        bool expireStudentSubscriptions,
        StudentSubscription? defaultSubscription,
        CancellationToken cancellationToken = default)
    {
        if (removeTeachingAssignments)
        {
            var assignments = await _db.CourseTeachers
                .Where(x => x.TeacherUserId == user.Id)
                .ToListAsync(cancellationToken);
            _db.CourseTeachers.RemoveRange(assignments);
        }

        if (expireStudentSubscriptions)
        {
            var now = DateTime.UtcNow;
            var subscriptions = await _db.StudentSubscriptions
                .Where(x => x.StudentUserId == user.Id && x.Status == SubscriptionStatusNames.Active)
                .ToListAsync(cancellationToken);
            foreach (var subscription in subscriptions)
            {
                subscription.Status = SubscriptionStatusNames.Expired;
                subscription.UpdatedAtUtc = now;
            }
        }

        if (defaultSubscription is not null)
        {
            _db.StudentSubscriptions.Add(defaultSubscription);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users.CountAsync(
            x => x.Role == UserRoleNames.Admin && !x.IsLockedOut,
            cancellationToken);
    }

    public async Task<bool> HasRelatedDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.StudentSubscriptions.AnyAsync(x => x.StudentUserId == userId, cancellationToken)
            || await _db.PaymentTransactions.AnyAsync(x => x.StudentUserId == userId, cancellationToken)
            || await _db.ChatSessions.AnyAsync(x => x.UserId == userId, cancellationToken)
            || await _db.Documents.AnyAsync(x => x.UploadedByUserId == userId, cancellationToken)
            || await _db.CourseTeachers.AnyAsync(x => x.TeacherUserId == userId, cancellationToken);
    }

    public async Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
