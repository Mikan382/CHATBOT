namespace DataAccessLayer.Repositories;

public record UserListRow(Guid Id, string Email, string DisplayName, string Role, bool IsLockedOut);
