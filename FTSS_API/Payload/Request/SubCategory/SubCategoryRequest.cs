namespace FTSS_API.Payload.Request.SubCategory
{
    public class SubCategoryRequest
    {
        public string SubCategoryName { get; set; } = null!;

        public Guid CategoryId { get; set; }

        public string? Description { get; set; }
    }
}
