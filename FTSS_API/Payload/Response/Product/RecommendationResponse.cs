using FTSS_API.Payload.Request.Product;

namespace FTSS_API.Payload.Response;

public class RecommendationResponse
{
    public Recommendations Recommendations { get; set; } = new Recommendations();
}