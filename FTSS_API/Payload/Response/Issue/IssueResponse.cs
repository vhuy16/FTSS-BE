namespace FTSS_API.Payload.Response.Issue;

public class IssueResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string IssueName { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }

    // Danh sách sản phẩm liên quan
    public List<IssueProductResponse> RelatedProducts { get; set; }
}
