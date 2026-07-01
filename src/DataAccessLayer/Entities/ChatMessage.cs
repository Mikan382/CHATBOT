using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public ChatRole Role { get; set; }
    public ModelType ModelType { get; set; }
    public string Content { get; set; } = "";
    public string? CitationsJson { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
