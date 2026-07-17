namespace BusinessLayer.Retrieval;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int TopK { get; set; }
    public double MinimumSimilarityScore { get; set; }
    public int HistoryMessageCount { get; set; }
}
