namespace FTSS_API.Payload.Response.Book
{
    public class GetListBookingForUserResponse
    {
        public Guid Id { get; set; }

        public DateTime? ScheduleDate { get; set; }

        public string? Status { get; set; }
        public string? MissionStatus { get; set; }
        public string? Address { get; set; }
        public string? BookingCode { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal? TotalPrice { get; set; }
        public Guid? OrderId { get; set; }

        public bool? IsAssigned { get; set; }
        public List<ServicePackageResponse> Services { get; set; } = new();
    }
}
