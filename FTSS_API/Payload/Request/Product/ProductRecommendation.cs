namespace FTSS_API.Payload.Request.Product;

public class ProductRecommendation
{
    public Guid Id { get; set; }
    public string ProductName { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Size { get; set; }
    public string SubCategoryName { get; set; }
    public string CategoryName { get; set; }
    public List<string> Images { get; set; }
    public int Power { get; set; }
}