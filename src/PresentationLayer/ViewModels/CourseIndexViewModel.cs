using BusinessLayer.DTOs;

namespace PresentationLayer.ViewModels;

public class CourseIndexViewModel
{
    public IReadOnlyList<CourseListDto> Courses { get; set; } = [];
    public string? SearchTerm { get; set; }
    public bool CanManageCourses { get; set; }
}
