namespace FTSS_API.Payload.Request.Book
{
    public class AssignTechBookingRequest
    {
        public string MissionName { get; set; } = null!;

        public string? MissionDescription { get; set; }
    }
}
