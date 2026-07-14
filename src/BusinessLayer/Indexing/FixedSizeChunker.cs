using System.Text.RegularExpressions;

namespace BusinessLayer.Indexing;

public class FixedSizeChunker : ITextChunker
{
    public const int DefaultChunkSize = 1000;
    public const int DefaultOverlap = 150;
    public const int MinChunkSize = 200;
    public const int MaxChunkSize = 4000;
    public const int MaxOverlap = 1000;

    private readonly int _chunkSize;
    private readonly int _overlap;

    public FixedSizeChunker(int chunkSize = DefaultChunkSize, int overlap = DefaultOverlap)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");
        }

        if (overlap < 0 || overlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be non-negative and smaller than chunk size.");
        }

        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public string StrategyName => "fixed";
    public int ChunkSize => _chunkSize;
    public int Overlap => _overlap;
    public string ConfigurationName => $"fixed_{_chunkSize}_overlap_{_overlap}";

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
