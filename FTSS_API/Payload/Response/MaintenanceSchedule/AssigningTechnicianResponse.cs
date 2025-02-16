namespace FTSS_API.Payload.Response.MaintenanceSchedule
{
    public class AssigningTechnicianResponse
    {
        public Guid Id { get; set; }

        public Guid MaintenanceScheduleId { get; set; }

        public string TaskName { get; set; } = null!;

        public string? TaskDescription { get; set; }
        public Guid TechnicianId { get; set; }
        public string TechnicianUserName { get; set; } = null!;

        public string? Status { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = null!;
        public string? Address { get; set; }

        public DateTime? ScheduleDate { get; set; }
    }
}
