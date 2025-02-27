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
        public string? Size { get; set; }
        public string LinkImage { get; set; } = null!;
        public List<ProductResponse> Products { get; set; } = new();
    }

    public class ProductResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
        public int? Quantity { get; set; }
        public string? Status { get; set; }
        public bool? IsDelete { get; set; }
        public string CategoryName { get; set; } = null!;

    }
}
