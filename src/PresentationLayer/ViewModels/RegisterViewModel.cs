using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(128)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}
