using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using DataAccessLayer.Enums;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Payment;

// VNPay 2.1.0 integration. Signing follows the official VnPayLibrary sample: sort the vnp_* fields
// by name (ordinal), URL-encode both key and value, join with '&', then HMAC-SHA512 with the shared
// HashSecret. The same routine builds the checkout URL and verifies return/IPN callbacks.
public class VnPayGateway : IPaymentGateway
{
    private readonly VnPayOptions _options;

    public VnPayGateway(IOptions<VnPayOptions> options)
    {
        _options = options.Value;
    }

    public string ProviderName => PaymentProviderNames.VnPay;
    public TimeSpan CheckoutLifetime => TimeSpan.FromMinutes(_options.PaymentTimeoutMinutes);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.TmnCode)
        && !string.IsNullOrWhiteSpace(_options.HashSecret)
        && Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(_options.Version)
        && !string.IsNullOrWhiteSpace(_options.Locale)
        && !string.IsNullOrWhiteSpace(_options.CurrCode)
        && _options.PaymentTimeoutMinutes is >= 1 and <= 60;

    public string BuildCheckoutUrl(PaymentCheckoutRequest request)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("VNPay is not configured. Set VnPay:TmnCode and VnPay:HashSecret.");
        }

        var createDate = VietnamTime(request.CreatedAtUtc);
        var expireDate = VietnamTime(request.ExpiresAtUtc);
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _options.TmnCode,
            // VNPay expects the amount in the smallest currency unit (VND * 100).
            ["vnp_Amount"] = (request.AmountVnd * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = _options.CurrCode,
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_Locale"] = _options.Locale,
            ["vnp_OrderInfo"] = request.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = request.ReturnUrl,
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"),
            ["vnp_TxnRef"] = request.TxnRef
        };

        var hashData = new StringBuilder();
        foreach (var (key, value) in fields)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (hashData.Length > 0) hashData.Append('&');
            hashData.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value));
        }

        var secureHash = HmacSha512(_options.HashSecret, hashData.ToString());
        return $"{_options.BaseUrl}?{hashData}&vnp_SecureHash={secureHash}";
    }

    public PaymentCallbackResult VerifyCallback(IReadOnlyDictionary<string, string> query)
    {
        var receivedHash = query.TryGetValue("vnp_SecureHash", out var h) ? h : "";
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith("vnp_", StringComparison.Ordinal)) continue;
            if (key is "vnp_SecureHash" or "vnp_SecureHashType") continue;
            fields[key] = value;
        }

        var hashData = new StringBuilder();
        foreach (var (key, value) in fields)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (hashData.Length > 0) hashData.Append('&');
            hashData.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value));
        }

        var computed = HmacSha512(_options.HashSecret, hashData.ToString());
        var signatureValid = IsConfigured
            && !string.IsNullOrEmpty(receivedHash)
            && FixedTimeEqualsHex(computed, receivedHash);

        _ = long.TryParse(
            fields.GetValueOrDefault("vnp_Amount"),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var amount);

        return new PaymentCallbackResult(
            signatureValid,
            fields.GetValueOrDefault("vnp_TxnRef", ""),
            fields.GetValueOrDefault("vnp_ResponseCode", ""),
            fields.GetValueOrDefault("vnp_TransactionStatus", ""),
            amount,
            fields.GetValueOrDefault("vnp_TransactionNo"));
    }

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEqualsHex(string expectedHex, string actualHex)
    {
        try
        {
            var expected = Convert.FromHexString(expectedHex);
            var actual = Convert.FromHexString(actualHex);
            return expected.Length == actual.Length
                && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static DateTime VietnamTime(DateTime utc)
    {
        var timeZoneId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);
    }
}
