namespace BusinessLayer.Parsing;

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken);
}
