using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Drawing = DocumentFormat.OpenXml.Drawing;
using Presentation = DocumentFormat.OpenXml.Presentation;
using UglyToad.PdfPig;

namespace Prn222Chatbot.Web.Services.Parsing;

public class DocumentTextExtractor : IDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".pptx",
        ".txt",
        ".md"
    };

    public async Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PDF, DOCX, PPTX, TXT, or MD uploads are supported.");
        }

        await using var stream = file.OpenReadStream();
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            ".pptx" => ExtractPptx(stream),
            ".txt" or ".md" => await ExtractTextAsync(stream, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported file format.")
        };
    }

    private static string ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart?.Document?.Body?.InnerText ?? "";
    }

    private static string ExtractPptx(Stream stream)
    {
        using var presentation = PresentationDocument.Open(stream, false);
        var presentationPart = presentation.PresentationPart;
        var slideIds = presentationPart?.Presentation?.SlideIdList?.Elements<Presentation.SlideId>();
        if (presentationPart is null || slideIds is null)
        {
            return "";
        }

        var builder = new StringBuilder();
        foreach (var slideId in slideIds)
        {
            var relationshipId = slideId.RelationshipId?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                continue;
            }

            if (presentationPart.GetPartById(relationshipId) is not SlidePart slidePart)
            {
                continue;
            }

            if (slidePart.Slide is null)
            {
                continue;
            }

            var slideText = slidePart.Slide
                .Descendants<Drawing.Text>()
                .Select(x => x.Text)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var text in slideText)
            {
                builder.AppendLine(text);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static async Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
