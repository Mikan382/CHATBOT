using System.Globalization;
using System.Text;

namespace Prn222Chatbot.Web.Services.Retrieval;

public static class TextNormalizer
{
    public static string Normalize(string input)
    {
        var normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(ch == '\u0111' ? 'd' : ch);
        }

        return new string(builder
            .ToString()
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
