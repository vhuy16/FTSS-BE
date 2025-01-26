namespace FTSS_API.Payload.Pay;

public class PayOsWebhookPayload
{
    public PaymentWebhookData Data { get; set; }
    public string Signature { get; set; }
}