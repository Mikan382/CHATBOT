namespace DataAccessLayer.Entities;

public class EvaluationQuestion
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid ExpectedDocumentId { get; set; }
    public string ExpectedSourceName { get; set; } = "";
    public string Question { get; set; } = "";
    public string ExpectedAnswer { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
