namespace FTSS_API.Payload.Request.Product;

public class Recommendations
{
    public ProductRecommendation Filter { get; set; }
    public ProductRecommendation Light { get; set; }
    public ProductRecommendation Substrate { get; set; }
    public List<ProductRecommendation> OtherProducts { get; set; } = new List<ProductRecommendation>();
}