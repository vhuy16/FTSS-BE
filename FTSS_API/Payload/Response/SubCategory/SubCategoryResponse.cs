namespace FTSS_API.Payload.Response.SubCategory
{
    public class SubCategoryResponse
    {
        public Guid Id { get; set; }

        public string SubCategoryName { get; set; } = null!;

        public Guid CategoryId { get; set; }

        public string? Description { get; set; }

        public DateTime? CreateDate { get; set; }

        public DateTime? ModifyDate { get; set; }
        public string CategoryName { get; set; } = null!;
    }
}
