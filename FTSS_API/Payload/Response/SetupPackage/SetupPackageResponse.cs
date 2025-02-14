namespace FTSS_API.Payload.Response.SetupPackage
{
    public class SetupPackageResponse
    {
        public Guid SetupPackageId { get; set; }
        public string SetupName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public List<ProductResponse> Products { get; set; } = new();
    }

    public class ProductResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
