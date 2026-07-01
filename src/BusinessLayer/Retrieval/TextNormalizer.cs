using System.Globalization;
using System.Text;

namespace BusinessLayer.Retrieval;

public static class TextNormalizer
{
    public static string Normalize(string input)
    {
        return new string(input
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormC)
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray());
    }

    public static IReadOnlyList<string> Terms(string input)
    {
        return Normalize(input)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length >= 2)
            .ToList();
    }
}
