namespace BusinessLayer.Payment;

public class VnPayOptions
{
    // Secrets (TmnCode, HashSecret) come from User Secrets / env, not appsettings.
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    // Where VNPay redirects the browser back to after payment (our VnpayReturn action).
    public string ReturnUrl { get; set; } = "";
    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";
    public string CurrCode { get; set; } = "VND";
}
