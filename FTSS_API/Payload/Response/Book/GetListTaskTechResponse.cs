using FTSS_API.Payload.Response.SetupPackage;

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
        public string FullName { get; set; } = null!;

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
        public SetupPackageResponse SetupPackage { get; set; }
    }
}
