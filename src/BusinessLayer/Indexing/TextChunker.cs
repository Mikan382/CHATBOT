using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLayer.Indexing;

public class TextChunker
{
    private const int TargetMin = 800;
    private const int TargetMax = 1200;
    private const int Overlap = 150;

    public IReadOnlyList<string> Chunk(string text)
    {
        var paragraphs = Regex.Split(text.Replace("\r\n", "\n"), @"\n{2,}")
            .Select(p => Regex.Replace(p.Trim(), @"\s+", " "))
            .Where(p => p.Length > 0)
            .ToList();

        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > TargetMax)
            {
                FlushCurrent(chunks, current);
                chunks.AddRange(SplitLongParagraph(paragraph));
                continue;
            }

            if (current.Length > 0 && current.Length + paragraph.Length + 2 > TargetMax)
            {
                FlushCurrent(chunks, current);
            }

            if (current.Length > 0)
            {
                current.AppendLine().AppendLine();
            }

            current.Append(paragraph);

            if (current.Length >= TargetMin)
            {
                FlushCurrent(chunks, current);
            }
        }

        FlushCurrent(chunks, current);
        return chunks.Where(c => c.Length > 40).ToArray();
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

    private static IEnumerable<string> SplitLongParagraph(string paragraph)
    {
        var start = 0;
        while (start < paragraph.Length)
        {
            var length = Math.Min(TargetMax, paragraph.Length - start);
            yield return paragraph.Substring(start, length).Trim();
            if (start + length >= paragraph.Length)
            {
                break;
            }

            start += Math.Max(1, length - Overlap);
        }
    }
}
