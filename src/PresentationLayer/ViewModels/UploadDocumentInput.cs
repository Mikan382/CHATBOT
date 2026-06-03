using System.ComponentModel.DataAnnotations;
using PresentationLayer.ViewModels.Validation;

namespace PresentationLayer.ViewModels;

public class UploadDocumentInput
{
    [Required(ErrorMessage = "Chapter is required.")]
    public Guid? ChapterId { get; set; }

    [Required(ErrorMessage = "File is required.")]
    [AllowedFileExtensions(".pdf", ".docx", ".pptx", ".txt", ".md")]
    [MaxFileSize(20 * 1024 * 1024)]
    public IFormFile? File { get; set; }
}
