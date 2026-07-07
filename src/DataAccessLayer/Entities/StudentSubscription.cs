namespace DataAccessLayer.Entities;

public class StudentSubscription
{
    public Guid Id { get; set; }
    public Guid StudentUserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ApplicationUser? Student { get; set; }
    public SubscriptionPlan? Plan { get; set; }
}
