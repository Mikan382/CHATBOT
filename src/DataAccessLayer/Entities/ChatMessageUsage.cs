namespace DataAccessLayer.Entities;

// Append-only usage meter for chat quota. Decoupled from ChatMessage rows on purpose:
// deleting or clearing a chat session must NOT refund quota, so usage is tracked here
// and only ever incremented.
public class ChatMessageUsage
{
    public Guid Id { get; set; }
    public Guid StudentUserId { get; set; }
    // Identifies the entitlement snapshot that owns this usage: "SUB-{subscriptionId:N}".
    public string PeriodKey { get; set; } = "";
    public int Count { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
