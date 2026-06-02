namespace Prn222Chatbot.Web.Domain;

public class Chapter
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public int Order { get; set; }
    public string Clo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<EvaluationQuestion> EvaluationQuestions { get; set; } = new List<EvaluationQuestion>();
}
