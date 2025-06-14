﻿namespace FTSS_API.Payload.Response.SetupPackage
{
    public class SetupPackageResponse
    {
        public Guid? SetupPackageId { get; set; }
        public string SetupName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? Size { get; set; }
        public string images { get; set; } = null!;
        public bool? IsDelete { get; set; }
        public List<ProductResponse> Products { get; set; } = new();
    }

    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Size { get; set; }
        public decimal Price { get; set; }
        public int? Quantity { get; set; }
        public int? InventoryQuantity { get; set; }
        public string? Status { get; set; }
        public bool? IsDelete { get; set; }
        public string CategoryName { get; set; } = null!;
        public string images { get; set; } = null!;

    }
}
