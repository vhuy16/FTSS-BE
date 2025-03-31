using FTSS_API.Payload.Response.SetupPackage;

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
        public Guid? OrderId { get; set; }
        public string? bookingCode { get; set; }
        public bool? IsAssigned { get; set; }
        public List<ServicePackageResponse> Services { get; set; } = new();
    }
    public class ServicePackageResponse
    {
        public Guid Id { get; set; }

        public string ServiceName { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
