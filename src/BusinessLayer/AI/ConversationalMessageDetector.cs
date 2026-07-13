using System.Globalization;
using System.Text;

namespace BusinessLayer.AI;

public static class ConversationalMessageDetector
{
    private static readonly HashSet<string> ConversationalTokens = new(StringComparer.Ordinal)
    {
        "a", "ah", "oh", "uh", "um", "hm", "hmm",
        "hi", "hey", "hello", "yo", "sup", "bro",
        "ok", "okay", "oke", "k", "kk", "roger",
        "yes", "no", "yeah", "yep", "nope", "yup",
        "thanks", "thank", "thankyou", "thx", "ty",
        "bye", "goodbye", "good", "morning", "afternoon", "evening", "night",
        "nice", "cool", "great", "fine", "sure", "alright", "right",
        "xin", "chao", "ban", "nhe", "nha", "cam", "on", "camon", "tks",
        "tam", "biet", "vui", "ha", "hehe", "haha", "hihi", "lol",
        "duoc", "roi", "da", "dung", "uk", "uhm", "oi", "e", "u", "woah", "wow"
    };

    private static readonly string[] QuestionIndicators =
    [
        "la gi", "la sao", "nhu the nao", "tai sao", "khi nao", "o dau",
        "what is", "what are", "how to", "how do", "why", "when", "where",
        "explain", "huong dan", "giup", "cho minh", "cho toi", "lam sao",
        "co gi", "khac nhau"
    ];

    public static bool IsConversationalOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = Collapse(NormalizeForDetection(text));
        if (text.Contains('?') || QuestionIndicators.Any(normalized.Contains))
        {
            return false;
        }

        var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return terms.Length is > 0 and <= 6 && terms.All(ConversationalTokens.Contains);
    }

    private static string NormalizeForDetection(string text)
    {
        return new string(text
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD)
            .Where(ch => char.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray());
    }

    private static string Collapse(string normalized)
    {
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
