using FTSS_API.Payload.Response.SetupPackage;

namespace FTSS_API.Payload.Response.Order
{
    public class GetOrderResponse
    {
        public Guid Id { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Status { get; set; }
        public decimal? ShipCost { get; set; }
        public DateTime? CreateDate { get; set; }
        public bool? IsEligible {get; set;}
        public DateTime? ModifyDate { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BuyerName { get; set; }
        
        public decimal Discount { get; set; }
        public UserResponse userResponse { get; set; } // Fixed property declaration and removed invalid initialization
        public List<OrderDetailCreateResponse> OrderDetails { get; set; } = new List<OrderDetailCreateResponse>();
        public PaymentResponse Payment { get; set; } = new PaymentResponse();
        public SetupPackageResponse? SetupPackage { get; set; } = new SetupPackageResponse();
        public class OrderDetailCreateResponse
        {
            public string? ProductName { get; set; }
            
            public string? CategoryName { get; set; }
            public string? SubCategoryName { get; set; }
            public decimal? Price { get; set; }
            public int? Quantity { get; set; }
            public string LinkImage { get; set; } = null!;
        }

        public class UserResponse
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string? PhoneNumber { get; set; }
        }

        public class PaymentResponse
        {
            public string PaymentMethod { get; set; } = null!;
            public string PaymentStatus { get; set; } = null!;
        }
    }
}
