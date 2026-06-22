namespace BusinessLayer.Indexing;

public interface ITextChunker
{
    string StrategyName { get; }
    IReadOnlyList<string> Chunk(string text);
}
