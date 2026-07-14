namespace DataAccessLayer.Entities;

public class BenchmarkRun
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string ChunkingStrategy { get; set; } = "";
    public string EmbeddingModelName { get; set; } = "";
    public int TopK { get; set; }
    public int QuestionCount { get; set; }
    public int ChunkCount { get; set; }
    public long DurationMilliseconds { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<BenchmarkResult> Results { get; set; } = new List<BenchmarkResult>();
}
