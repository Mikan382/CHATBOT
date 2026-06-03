using DataAccessLayer.Entities;

namespace PresentationLayer.ViewModels;

public class DocumentIndexViewModel
{
    public IReadOnlyList<Chapter> Chapters { get; set; } = [];
    public IReadOnlyList<Document> Documents { get; set; } = [];
    public string? SearchTerm { get; set; }
    public Guid? SelectedChapterId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
