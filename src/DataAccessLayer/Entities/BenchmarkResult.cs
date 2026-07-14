namespace DataAccessLayer.Entities;

public class BenchmarkResult
{
    public Guid Id { get; set; }
    public Guid BenchmarkRunId { get; set; }
    public BenchmarkRun? BenchmarkRun { get; set; }
    public Guid EvaluationQuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string Question { get; set; } = "";
    public string ExpectedAnswer { get; set; } = "";
    public Guid ExpectedDocumentId { get; set; }
    public string ExpectedSourceName { get; set; } = "";
    public string GeneratedAnswer { get; set; } = "";
    public string RetrievedSourcesJson { get; set; } = "[]";
    public bool HitAtK { get; set; }
    public double ReciprocalRank { get; set; }
    public double AnswerTokenF1 { get; set; }
    public long LatencyMilliseconds { get; set; }
}
