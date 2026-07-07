using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IUserAdminRepository
{
    Task<IReadOnlyList<UserListRow>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUser>> ListUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
