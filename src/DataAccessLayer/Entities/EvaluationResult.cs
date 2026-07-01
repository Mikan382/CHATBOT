namespace DataAccessLayer.Entities;

public class EvaluationResult
{
    public Guid Id { get; set; }
    public Guid EvaluationQuestionId { get; set; }
    public EvaluationQuestion? EvaluationQuestion { get; set; }
    public string RagAnswer { get; set; } = "";
    public string? FineTunedAnswer { get; set; }
    public string RetrievedChunksJson { get; set; } = "[]";
    public decimal Faithfulness { get; set; }
    public decimal AnswerRelevance { get; set; }
    public decimal RetrievalRecall { get; set; }
    public decimal CitationAccuracy { get; set; }
    public string Status { get; set; } = "Completed";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Research metadata
    public string ChunkingStrategy { get; set; } = "paragraph";
    public string EmbeddingModelName { get; set; } = "";
    public int RagLatencyMs { get; set; }
    public int FineTunedLatencyMs { get; set; }

    // Fine-tuned scoring (parallel to RAG scoring)
    public decimal FtFaithfulness { get; set; }
    public decimal FtAnswerRelevance { get; set; }
}
