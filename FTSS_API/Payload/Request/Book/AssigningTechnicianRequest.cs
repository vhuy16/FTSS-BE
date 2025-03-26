namespace FTSS_API.Payload.Request.MaintenanceSchedule
{
    public class AssigningTechnicianRequest
    {
        public Guid TechnicianId { get; set; }
        public Guid OrderId { get; set; }
        public string MissionName { get; set; } = null!;

        public string? MissionDescription { get; set; }
        public DateTime? MissionSchedule { get; set; }
    }
}
