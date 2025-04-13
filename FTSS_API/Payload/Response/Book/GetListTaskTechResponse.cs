namespace FTSS_API.Payload.Response.Book
{
    public class GetListTaskTechResponse
    {
        public Guid Id { get; set; }
        public string MissionName { get; set; } = null!;
        public string? MissionDescription { get; set; }
        public string? Status { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? MissionSchedule { get; set; }
        public DateTime? EndMissionSchedule { get; set; }
        public string? CancelReason { get; set; }

        public string FullName { get; set; } = null!;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }

        public Guid? BookingId { get; set; }
        public Guid? OrderId { get; set; }

        public string? BookingCode { get; set; }
        public string? BookingImage { get; set; }

        public string? OrderCode { get; set; }
        public DateTime? InstallationDate { get; set; }

        public List<ServicePackageResponse> Services { get; set; } = new();
    }

}
