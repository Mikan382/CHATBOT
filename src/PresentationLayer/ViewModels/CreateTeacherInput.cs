using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels;

public class CreateTeacherInput
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(128)]
    public string FullName { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";
}
