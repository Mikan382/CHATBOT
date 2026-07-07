namespace BusinessLayer.Services;

public interface IAuthService
{
    Task SignInAsync(string email, string password, bool rememberMe);
    Task SignOutAsync();
    Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
