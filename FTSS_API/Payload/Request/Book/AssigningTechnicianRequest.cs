namespace FTSS_API.Payload.Request.MaintenanceSchedule
{
    public class AssigningTechnicianRequest
    {
        public string TaskName { get; set; } = null!;

        public string? TaskDescription { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public string? Address { get; set; }
    }
}
