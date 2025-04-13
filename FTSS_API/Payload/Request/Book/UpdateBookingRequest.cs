namespace FTSS_API.Payload.Request.Book
{
    public class UpdateBookingRequest
    {
        public DateTime? ScheduleDate { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public List<IFormFile>? ImageLink { get; set; } = new List<IFormFile>();
    }
}
