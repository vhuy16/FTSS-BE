namespace FTSS_API.Payload.Request.Product;

public class Recommendations
{
    public List<ProductRecommendation> Filters { get; set; } = new List<ProductRecommendation>();
    public List<ProductRecommendation> Lights { get; set; } = new List<ProductRecommendation>();
    public List<ProductRecommendation> Substrates { get; set; } = new List<ProductRecommendation>();
}