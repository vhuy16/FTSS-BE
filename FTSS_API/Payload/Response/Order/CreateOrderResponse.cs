namespace FTSS_API.Payload.Response.Order;

public class CreateOrderResponse
{
    public Guid Id { get; set; }
    public decimal? TotalPrice { get; set; }
    public int? ShipCost { get; set; }
    public string? OrderCode { get; set; }
    public string? Address { get; set; }
    public Guid? SetupPackageId { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsEligible  {get; set;}

    public string? RecipientName { get; set; }
    public UserResponse userResponse { get; set; } // Fixed property declaration and removed invalid initialization
    public List<OrderDetailCreateResponse> OrderDetails { get; set; } = new List<OrderDetailCreateResponse>();
    public string? CheckoutUrl { get; set; }
    public string? Description {get; set;}
    public class OrderDetailCreateResponse
    {
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
    }

    public class UserResponse
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

  
}