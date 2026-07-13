using BusinessLayer.Services;

namespace PresentationLayer.ViewModels;

public class DocumentIndexViewModel
{
    public IReadOnlyList<ChapterSelectDto> Chapters { get; set; } = [];
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public IReadOnlyList<DocumentIndexDto> Documents { get; set; } = [];
    public string? SearchTerm { get; set; }
    public Guid? SelectedCourseId { get; set; }
    public Guid? SelectedChapterId { get; set; }
    public bool CanManageDocuments { get; set; }
}
