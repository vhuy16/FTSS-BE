namespace FTSS_API.Payload.Response.IssueCategory
{
    public class IssueCategoryResponse
    {
        public Guid Id { get; set; }
        public string IssueCategoryName { get; set; }
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public bool IsDelete { get; set; }
    }
}