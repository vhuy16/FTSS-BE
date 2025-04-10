namespace FTSS_API.Payload.Response.Payment;

public class PaymentDto
{
    public Guid? Id { get; set; }
    public string? PaymentStatus { get; set; }
    public Guid? OrderId { get; set; } // Chỉ cần OrderId, không cần toàn bộ Order
}