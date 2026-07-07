namespace BusinessLayer.Services;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListDto>> ListAsync();
    Task CreateAsync(string email, string fullName, string role, string password);
    Task ChangeRoleAsync(Guid userId, string role);
    Task SetLockoutAsync(Guid userId, bool locked);
    Task DeleteAsync(Guid userId);
    Task ResetPasswordAsync(Guid userId, string newPassword);
}
