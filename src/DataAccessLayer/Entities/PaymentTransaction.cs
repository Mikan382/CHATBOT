namespace DataAccessLayer.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid StudentUserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }

    // Set only when a successful payment activates a subscription. Null while Pending/Failed.
    public Guid? StudentSubscriptionId { get; set; }

    public string Provider { get; set; } = "";

    // The merchant-side reference echoed back by the gateway (vnp_TxnRef). Unique.
    public string ProviderTxnRef { get; set; } = "";
    public decimal Amount { get; set; }
    public int DurationDays { get; set; }
    public int MessageQuota { get; set; }

    // Pending / Paid / Failed; stale Pending rows are displayed as Expired.
    public string Status { get; set; } = "";

    // Gateway-side transaction id (vnp_TransactionNo) and result code (vnp_ResponseCode).
    public string? ProviderTransactionNo { get; set; }
    public string? ResponseCode { get; set; }

    // Full callback payload kept for audit / dispute tracing.
    public string? RawResponse { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Student { get; set; }
    public SubscriptionPlan? Plan { get; set; }
}
