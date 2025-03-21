namespace FTSS_API.Payload.Request.Book
{
    public class BookingScheduleRequest
    {
        public DateTime? ScheduleDate { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
