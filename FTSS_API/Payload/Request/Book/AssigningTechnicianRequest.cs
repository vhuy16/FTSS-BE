namespace FTSS_API.Payload.Request.MaintenanceSchedule
{
    public class AssigningTechnicianRequest
    {
        public string MissionName { get; set; } = null!;

        public string? MissionDescription { get; set; }
        public DateTime? MissionSchedule { get; set; }
        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
