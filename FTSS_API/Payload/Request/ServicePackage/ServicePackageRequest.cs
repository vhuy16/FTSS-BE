namespace FTSS_API.Payload.Request.ServicePackage
{
    public class ServicePackageRequest
    {
        public string ServiceName { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }
    }
}
