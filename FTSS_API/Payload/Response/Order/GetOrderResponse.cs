using FTSS_API.Payload.Response.Book;
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
        public bool? IsAssigned {get; set;}
        public string? OderCode {get; set;}
        public DateTime? ModifyDate { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BuyerName { get; set; }
        public DateTime? InstallationDate { get; set; }
        public Guid? MissionId { get; set; }
        public string? CancelReason { get; set; }
        public string? ReportReason { get; set; }
        public List<MissionImageResponse> Images { get; set; } = new();



        public UserResponse userResponse { get; set; } // Fixed property declaration and removed invalid initialization
        public List<OrderDetailCreateResponse> OrderDetails { get; set; } = new List<OrderDetailCreateResponse>();
        public PaymentResponse Payment { get; set; } = new PaymentResponse();
        public VoucherResponse Voucher { get; set; } = new VoucherResponse();
        public SetupPackageResponse? SetupPackage { get; set; } = new SetupPackageResponse();
        public List<ReturnRequestResponse> ReturnRequests { get; set; }
        public class OrderDetailCreateResponse
        {
            public string? ProductName { get; set; }
            
            public string? CategoryName { get; set; }
            public string? SubCategoryName { get; set; }
            public decimal? Price { get; set; }
            public int? Quantity { get; set; }
            public string LinkImage { get; set; } = null!;
        }

        public class VoucherResponse
        {
            public string VoucherCode { get; set; } = null!;
            public string DiscountType { get; set; } = null!;
            public decimal? Discount { get; set; }
            public decimal? MaximumOrderValue  {get; set;}
        }

        public class UserResponse
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string? PhoneNumber { get; set; }
        }
        public class ReturnRequestResponse
        {
            public Guid Id { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<ReturnRequestMediaResponse> MediaFiles { get; set; }
        }

        public class ReturnRequestMediaResponse
        {
            public Guid Id { get; set; }
            public string MediaLink { get; set; }
            public string MediaType { get; set; }
        }

        public class PaymentResponse
        {
            public Guid PaymentId { get; set; }
            public string PaymentMethod { get; set; } = null!;
            public string PaymentStatus { get; set; } = null!;
            public string? BankNumber { get; set; }

            public string? BankName { get; set; }

            public string? BankHolder { get; set; }
        }
    }
}
