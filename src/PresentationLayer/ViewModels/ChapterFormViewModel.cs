using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels;

public class ChapterFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Range(1, 999)]
    public int Order { get; set; } = 1;

    [StringLength(32)]
    public string? Clo { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = "";

    public string? Summary { get; set; }
    public string? Error { get; set; }
}
