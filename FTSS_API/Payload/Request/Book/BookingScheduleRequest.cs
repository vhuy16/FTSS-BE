namespace FTSS_API.Payload.Request.Book
{
    public class BookingScheduleRequest
    {
        public Guid OrderId { get; set; }
        public DateTime? ScheduleDate { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
        public List<ServiceItems>? ServiceIds { get; set; }
        public string? PaymentMethod { get; set; }
    }
    public class ServiceItems
    {
        public Guid ServiceId { get; set; }
    }
}
