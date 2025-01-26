namespace FTSS_API.Payload.Request.Pay;

public class CreatePaymentRequest
{
    public string? PaymentMethod { get; set; }
   
    public Guid OrderId { get; set; }
}