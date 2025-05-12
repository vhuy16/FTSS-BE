namespace FTSS_API.Payload.Response.Book
{
    public class GetServicePackageResponse
    {
        public Guid Id { get; set; }

        public string ServiceName { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? Status { get; set; }

        public bool? IsDelete { get; set; }
    }
}
