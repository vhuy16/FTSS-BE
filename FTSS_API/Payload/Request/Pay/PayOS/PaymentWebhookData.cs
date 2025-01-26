namespace FTSS_API.Payload.Pay;

public class PaymentWebhookData
{
    public long OrderCode { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
}