namespace FTSS_API.Payload.Request;

public class AddUpdateIssueRequest
{
    public string Title { get; set; }
    public string IssueName { get; set; }
    public string Description { get; set; }
    public List<Guid> ProductIds { get; set; }  // Danh sách sản phẩm liên quan
}