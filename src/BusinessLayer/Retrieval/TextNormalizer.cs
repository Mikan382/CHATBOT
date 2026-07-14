using System.Globalization;
using System.Text;

namespace BusinessLayer.Retrieval;

public static class TextNormalizer
{
    private static readonly HashSet<string> TechnicalSingleCharacterTerms = ["c", "f"];

    public static string Normalize(string input)
    {
        var normalized = new string(input
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormC)
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray());
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static IReadOnlyList<string> Terms(string input)
    {
        return Normalize(input)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length >= 2 || TechnicalSingleCharacterTerms.Contains(x))
            .ToList();
    }
}
