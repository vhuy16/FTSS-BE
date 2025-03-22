 namespace FTSS_API.Payload.Request;

public class CreateOrderRequest
{
    public List<Guid>? CartItem { get; set; }
    public int ShipCost { get; set; }
    public string Address { get; set; }
    public Guid? VoucherId { get; set; }
    public string PaymentMethod { get; set; }
    public string? PhoneNumber { get; set; }
    public string? RecipientName { get; set; }
    public Guid? SetupPackageId   { get; set; }
}