namespace BusinessLayer.Payment;

public record PaymentCheckoutRequest(string TxnRef, long AmountVnd, string OrderInfo, string IpAddress);

public record PaymentCallbackResult(
    bool SignatureValid,
    string TxnRef,
    string ResponseCode,
    string TransactionStatus,
    long Amount,
    string? TransactionNo);

public interface IPaymentGateway
{
    string ProviderName { get; }
    bool IsConfigured { get; }

    // Builds the signed redirect URL that sends the student to the gateway's hosted payment page.
    string BuildCheckoutUrl(PaymentCheckoutRequest request);

    // Recomputes the signature over the callback fields to prove the response really came from
    // the gateway before we trust ResponseCode.
    PaymentCallbackResult VerifyCallback(IReadOnlyDictionary<string, string> query);
}
