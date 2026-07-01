using System.ComponentModel.DataAnnotations;
using BusinessLayer.Services;

namespace PresentationLayer.ViewModels;

public class CourseFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [StringLength(512)]
    public string? Tools { get; set; }

    public string? Error { get; set; }
}

public class CourseIndexViewModel
{
    public IReadOnlyList<CourseListDto> Courses { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
