namespace FTSS_API.Payload.Response.Book
{
    public class AssignTechBookingResponse
    {
        public Guid Id { get; set; }
        public string MissionName { get; set; } = null!;
        public string? MissionDescription { get; set; }
        public Guid TechnicianId { get; set; }
        public string TechnicianName { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? MissionSchedule { get; set; }
        public Guid? BookingId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
