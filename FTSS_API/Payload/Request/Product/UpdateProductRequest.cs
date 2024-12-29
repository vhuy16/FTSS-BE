namespace FTSS_API.Payload.Request.Product;

public class UpdateProductRequest
{
    public string? ProductName { get; set; } 
    public Guid? SubcategoryId { get; set; } 
    public int? Quantity { get; set; } 
    public decimal? Price { get; set; }
    public string? Description { get; set; } 

    public string? Status { get; set; }
    public List<IFormFile>? ImageLink { get; set; } = new List<IFormFile>();
}