namespace Prn222Chatbot.Web.Services.Parsing;

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken);
}
