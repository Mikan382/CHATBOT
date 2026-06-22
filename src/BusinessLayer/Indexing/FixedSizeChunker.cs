using System.Text.RegularExpressions;

namespace BusinessLayer.Indexing;

public class FixedSizeChunker : ITextChunker
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public FixedSizeChunker(int chunkSize = 1000, int overlap = 150)
    {
        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public string StrategyName => $"fixed_{_chunkSize}";

    public IReadOnlyList<string> Chunk(string text)
    {
        var cleaned = Regex.Replace(text.Replace("\r\n", "\n"), @"\s+", " ").Trim();
        if (cleaned.Length == 0)
        {
            return [];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < cleaned.Length)
        {
            var length = Math.Min(_chunkSize, cleaned.Length - start);
            var chunk = cleaned.Substring(start, length).Trim();

            if (chunk.Length > 40)
            {
                chunks.Add(chunk);
            }

            if (start + length >= cleaned.Length)
            {
                break;
            }

            start += Math.Max(1, length - _overlap);
        }

        return chunks;
    }
}
