namespace FTSS_API.Payload.Response;

public class GetProductResponse
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? Quantity { get; set; }
   
    public string SubCategoryName { get; set; } = null!;
    public string CategoryName { get; set; }

    public decimal? Price { get; set; }
    
    public string Status { get; set; }
    public List<string> Images { get; set; } = new List<string>();
}