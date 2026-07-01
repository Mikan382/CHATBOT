namespace DataAccessLayer.Entities;

public class EvaluationQuestion
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public int Order { get; set; }
    public string Question { get; set; } = "";
    public string GroundTruth { get; set; } = "";
    public ICollection<EvaluationResult> Results { get; set; } = new List<EvaluationResult>();
}
