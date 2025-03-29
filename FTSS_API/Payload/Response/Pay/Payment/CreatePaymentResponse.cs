namespace FTSS_API.Payload.Response.Pay.Payment;

public class CreatePaymentResponse
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public decimal? AmoundPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentURL { get; set; }
    public long PaymentCode { get; set; }
    public string Description { get; set; }
}