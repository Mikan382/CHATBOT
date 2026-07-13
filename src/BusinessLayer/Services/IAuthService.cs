namespace BusinessLayer.Services;

public interface IAuthService
{
    Task<AuthenticatedUserDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<bool> IsPrincipalCurrentAsync(Guid userId, string email, string role, long userVersion, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
