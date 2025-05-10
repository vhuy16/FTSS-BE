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
        public Guid? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public string? BookingCode { get; set; }
        public DateTime? EndMissionSchedule { get; set; }
        public string? CancelReason { get; set; }

        public string FullName { get; set; } = null!;
        public string? BookingImage { get; set; }
        public DateTime? InstallationDate { get; set; }
        public List<MissionImageResponse> Images { get; set; } = new();

        public List<ServicePackageResponse> Services { get; set; } = new();
    }
}
