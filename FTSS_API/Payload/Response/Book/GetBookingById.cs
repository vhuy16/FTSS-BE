using FTSS_API.Payload.Response.SetupPackage;
using static FTSS_API.Payload.Response.Order.GetOrderResponse;

namespace FTSS_API.Payload.Response.Book
{
    public class GetBookingById
    {
        public Guid Id { get; set; }

        public DateTime? ScheduleDate { get; set; }

        public string? Status { get; set; }
        public Guid? UserId { get; set; }

        public string UserName { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal? TotalPrice { get; set; }
        
        public string? BookingCode { get; set; }
        public List<string>? ImageLinks { get; set; } = new();
        public Guid? OrderId { get; set; }
     
        public bool? IsAssigned { get; set; }
        public List<ServicePackageResponse> Services { get; set; } = new();
        public PaymentResponse Payment { get; set; } = new PaymentResponse();
        public SetupPackageResponse? SetupPackage { get; set; } = new SetupPackageResponse();
    }
    public class ServicePackageResponse
    {
        public Guid Id { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
