namespace FTSS_API.Payload.Request;

public class CreateOrderRequest
{
    public List<Guid> CartItem { get; set; }
    public int ShipCost { get; set; }
    public string Address { get; set; }
    public Guid VoucherId { get; set; }
}