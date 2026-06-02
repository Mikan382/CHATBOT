using System.ComponentModel.DataAnnotations;
using Prn222Chatbot.Web.ViewModels.Validation;

namespace Prn222Chatbot.Web.ViewModels;

public class UploadDocumentInput
{
    [Required(ErrorMessage = "Chapter is required.")]
    public Guid? ChapterId { get; set; }

    [Required(ErrorMessage = "File is required.")]
    [AllowedFileExtensions(".pdf", ".docx", ".txt", ".md")]
    [MaxFileSize(20 * 1024 * 1024)]
    public IFormFile? File { get; set; }
}
