namespace BusinessLayer.Parsing;

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken);
}
