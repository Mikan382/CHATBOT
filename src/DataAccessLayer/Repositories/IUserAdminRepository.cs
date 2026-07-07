using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;

namespace DataAccessLayer.Repositories;

public record UserListRow(Guid Id, string Email, string FullName, string Role, bool IsLockedOut);

public interface IUserAdminRepository
{
    Task<IReadOnlyList<UserListRow>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default);
}

public class UserAdminRepository : IUserAdminRepository
{
    private readonly AppDbContext _db;

    public UserAdminRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserListRow>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .OrderBy(x => x.Email)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await (
            from ur in _db.UserRoles
            join role in _db.Roles on ur.RoleId equals role.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, role.Name }
        ).ToListAsync(cancellationToken);

        var roleLookup = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name ?? "").Where(x => x.Length > 0).ToList());

        return users.Select(user => new UserListRow(
            user.Id,
            user.Email ?? "",
            user.FullName,
            roleLookup.TryGetValue(user.Id, out var roles) ? roles.FirstOrDefault() ?? "" : "",
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
            .ToList();
    }
}
