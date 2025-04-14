namespace FTSS_API.Payload.Request.Book
{
    public class UpdateMissionRequest
    {
        public string? MissionName { get; set; }

        public string? MissionDescription { get; set; }
        public Guid? TechnicianId { get; set; }
       
    }
}
