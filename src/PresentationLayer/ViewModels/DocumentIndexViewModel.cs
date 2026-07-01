using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using BusinessLayer.Services;

namespace PresentationLayer.ViewModels;

public class DocumentIndexViewModel
{
    public IReadOnlyList<Chapter> Chapters { get; set; } = [];
    public IReadOnlyList<CourseDto> Courses { get; set; } = [];
    public IReadOnlyList<Document> Documents { get; set; } = [];
    public string? SearchTerm { get; set; }
    public Guid? SelectedCourseId { get; set; }
    public Guid? SelectedChapterId { get; set; }
    public DocumentIndexStatus? SelectedStatus { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public bool CanManageDocuments { get; set; }
}
