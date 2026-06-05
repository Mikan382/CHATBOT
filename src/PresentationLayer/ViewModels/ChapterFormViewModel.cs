using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;

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
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public string? Error { get; set; }
}

public class CourseChaptersViewModel
{
    public CourseDto Course { get; set; } = new(Guid.Empty, "", "", "", "", []);
    public IReadOnlyList<ChapterDto> Chapters { get; set; } = [];
    public string? Message { get; set; }
    public string? Error { get; set; }
}
