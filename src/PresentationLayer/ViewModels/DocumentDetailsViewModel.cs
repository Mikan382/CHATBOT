using DataAccessLayer.Entities;

namespace PresentationLayer.ViewModels;

public class DocumentDetailsViewModel
{
    public Document Document { get; set; } = new();
    public bool CanManageDocuments { get; set; }
}
