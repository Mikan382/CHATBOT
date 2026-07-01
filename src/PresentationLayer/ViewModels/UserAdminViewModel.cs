using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;
using DataAccessLayer.Enums;

namespace PresentationLayer.ViewModels;

public class UserAdminIndexViewModel
{
    public IReadOnlyList<UserListDto> Users { get; set; } = [];
    public CreateUserInput CreateUser { get; set; } = new();
    public IReadOnlyList<string> Roles { get; set; } = [UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin];
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class CreateUserInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [StringLength(160)]
    public string FullName { get; set; } = "";

    [Required]
    public string Role { get; set; } = UserRoleNames.Student;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
