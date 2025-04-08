namespace FTSS_API.Payload.Response.Book
{
    public class GetListBookingForManagerResponse
    {
        public Guid Id { get; set; }

        public DateTime? ScheduleDate { get; set; }

        public string? Status { get; set; }
        public string? MissionStatus { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
        public string? bookingCode { get; set; }

        public decimal? TotalPrice { get; set; }

        public Guid? UserId { get; set; }
        public string UserName { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public Guid? OrderId { get; set; }

        public bool? IsAssigned { get; set; }
    }
}
