namespace FTSS_API.Payload.Request.IssueCategory;

public class AddUpdateIssueCategoryRequest
{
    public string IssueCategoryName { get; set; }
    public string? Description { get; set; }
}