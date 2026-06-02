namespace Prn222Chatbot.Web.Domain;

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
}
