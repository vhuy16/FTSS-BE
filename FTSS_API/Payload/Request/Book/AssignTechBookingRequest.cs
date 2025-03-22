namespace FTSS_API.Payload.Request.Book
{
    public class AssignTechBookingRequest
    {
        public Guid BookingId { get; set; }
        public Guid TechnicianId { get; set; }
        public string MissionName { get; set; } = null!;

        public string? MissionDescription { get; set; }
    }
}
