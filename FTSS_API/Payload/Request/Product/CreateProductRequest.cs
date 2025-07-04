﻿namespace FTSS_API.Payload.Request.Product;

public class CreateProductRequest
{
    public string ProductName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int? Power  { get; set; }
    public string? Size { get; set; } = null!;
    public Guid SubCategoryId { get; set; }

    public List<IFormFile> ImageLink { get; set; } = new List<IFormFile>();
}