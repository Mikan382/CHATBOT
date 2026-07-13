using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;

namespace PresentationLayer.ViewModels;

public class UserAdminIndexViewModel
{
    public IReadOnlyList<UserListDto> Users { get; set; } = [];
    public CreateUserInput CreateUser { get; set; } = new();
    public IReadOnlyList<string> Roles { get; set; } = ["Student", "Teacher", "Admin"];
    public string? SearchTerm { get; set; }
    public string? SelectedRole { get; set; }
    public Guid CurrentUserId { get; set; }
}

public class CreateUserInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(160)]
    public string FullName { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Student";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class UpdateUserInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(160)]
    public string FullName { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Student";
}
