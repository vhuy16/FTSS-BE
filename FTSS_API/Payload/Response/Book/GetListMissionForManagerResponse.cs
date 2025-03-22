namespace FTSS_API.Payload.Response.Book
{
    public class GetListMissionForManagerResponse
    {
        public Guid Id { get; set; }

        public string MissionName { get; set; } = null!;

        public string? MissionDescription { get; set; }

        public string? Status { get; set; }

        public DateTime? MissionSchedule { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
        public Guid? TechnicianId { get; set; }
        public string TechnicianName { get; set; } = null!;
        public Guid? BookingId { get; set; }
    }
}
