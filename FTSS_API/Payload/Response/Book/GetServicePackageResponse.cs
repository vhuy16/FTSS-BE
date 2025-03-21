namespace FTSS_API.Payload.Response.Book
{
    public class GetServicePackageResponse
    {
        public Guid Id { get; set; }

        public string ServiceName { get; set; } = null!;

        public decimal Price { get; set; }
    }
}
