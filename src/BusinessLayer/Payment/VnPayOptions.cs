namespace BusinessLayer.Payment;

public class VnPayOptions
{
    // Secrets (TmnCode, HashSecret) come from User Secrets / env, not appsettings.
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string Version { get; set; } = "";
    public string Locale { get; set; } = "";
    public string CurrCode { get; set; } = "";
    public int PaymentTimeoutMinutes { get; set; }
}
