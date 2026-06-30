using System.Globalization;
using System.Text;
using BusinessLayer.Retrieval;

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
        "xin", "chao", "ban", "nhe", "nha", "ak", "akay",
        "cam", "on", "camon", "tks",
        "tam", "biet", "vui", "vai", "ha", "hehe", "haha", "hihi", "lol",
        "duoc", "roi", "da", "dung", "uk", "uhm",
        "oi", "e", "u", "woah", "wow"
    };

    private static readonly string[] QuestionIndicators =
    [
        " la gi", " là gì", " la sao", " là sao", " nhu the nao", " như thế nào",
        " tai sao", " tại sao", " khi nao", " khi nào", " o dau", " ở đâu",
        " what is", " what are", " how to", " how do", " why ", " when ", " where ",
        " explain", " huong dan", " hướng dẫn", " giup", " giúp", " cho minh", " cho toi",
        "?", " lam sao", " làm sao", " co gi", " có gì", " khac nhau", " khác nhau"
    ];

    public static bool IsConversationalOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (ContainsQuestionIndicator(trimmed))
        {
            return false;
        }

        var normalized = Collapse(NormalizeForDetection(trimmed));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length > 6)
        {
            return false;
        }

        return terms.All(IsConversationalToken);
    }

    private static bool ContainsQuestionIndicator(string text)
    {
        var lower = text.ToLowerInvariant();
        foreach (var indicator in QuestionIndicators)
        {
            if (lower.Contains(indicator, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConversationalToken(string token)
    {
        if (ConversationalTokens.Contains(token))
        {
            return true;
        }

        return token.Length <= 2;
    }

    // Strip diacritics so "chào" → "chao", "bạn" → "ban", etc., matching the token set.
    // Kept separate from TextNormalizer.Normalize to avoid degrading Vietnamese lexical search.
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
