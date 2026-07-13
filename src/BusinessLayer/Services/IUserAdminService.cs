using BusinessLayer.DTOs;

namespace BusinessLayer.Services;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListDto>> ListAsync(string? searchTerm, string? role, CancellationToken cancellationToken);
    Task CreateAsync(string email, string fullName, string role, string password, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid actorUserId, Guid userId, string email, string fullName, string role, CancellationToken cancellationToken);
    Task SetLockoutAsync(Guid actorUserId, Guid userId, bool locked, CancellationToken cancellationToken);
    Task DeleteAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken);
    Task ResetPasswordAsync(Guid actorUserId, Guid userId, string newPassword, CancellationToken cancellationToken);
}
