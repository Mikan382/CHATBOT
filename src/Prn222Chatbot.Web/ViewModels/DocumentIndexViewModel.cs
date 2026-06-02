using Prn222Chatbot.Web.Domain;

namespace Prn222Chatbot.Web.ViewModels;

public class DocumentIndexViewModel
{
    public IReadOnlyList<Chapter> Chapters { get; set; } = [];
    public IReadOnlyList<Document> Documents { get; set; } = [];
    public string? Message { get; set; }
    public string? Error { get; set; }
}
