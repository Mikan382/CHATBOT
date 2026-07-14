using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLayer.Indexing;

public class SentenceChunker : ITextChunker
{
    private const int TargetMinChars = 600;
    private const int TargetMaxChars = 1000;

    public string StrategyName => "sentence";

    public IReadOnlyList<string> Chunk(string text)
    {
        var sentences = SplitIntoSentences(text);
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (sentence.Length > TargetMaxChars)
            {
                FlushCurrent(chunks, current);
                chunks.AddRange(SplitLongSentence(sentence));
                continue;
            }

            if (current.Length > 0 && current.Length + sentence.Length + 1 > TargetMaxChars)
            {
                FlushCurrent(chunks, current);
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(sentence);

            if (current.Length >= TargetMinChars)
            {
                FlushCurrent(chunks, current);
            }
        }

        FlushCurrent(chunks, current);
        return chunks.Where(c => c.Length > 40).ToArray();
    }

    private static IReadOnlyList<string> SplitIntoSentences(string text)
    {
        var cleaned = text.Replace("\r\n", "\n");

        // Split on sentence-ending punctuation followed by whitespace, or on double newlines
        var parts = Regex.Split(cleaned, @"(?<=[.!?;])\s+|(?:\n{2,})")
            .Select(s => Regex.Replace(s.Trim(), @"\s+", " "))
            .Where(s => s.Length > 0)
            .ToList();

        return parts;
    }

    private static IEnumerable<string> SplitLongSentence(string sentence)
    {
        var start = 0;
        while (start < sentence.Length)
        {
            var length = Math.Min(TargetMaxChars, sentence.Length - start);
            if (start + length < sentence.Length)
            {
                var lastSpace = sentence.LastIndexOf(' ', start + length - 1, length);
                if (lastSpace > start)
                {
                    length = lastSpace - start;
                }
            }

            yield return sentence.Substring(start, length).Trim();
            start += length;
            while (start < sentence.Length && sentence[start] == ' ')
            {
                start++;
            }
        }
    }

    private static void FlushCurrent(List<string> chunks, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(current.ToString().Trim());
        current.Clear();
    }
}
