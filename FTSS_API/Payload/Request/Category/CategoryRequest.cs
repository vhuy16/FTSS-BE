namespace FTSS_API.Payload.Request.Category
{
    public class CategoryRequest
    {
        // Tên danh mục (bắt buộc)
        public string CategoryName { get; set; } = null!;

        // Mô tả danh mục (không bắt buộc)
        public string? Description { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool? IsFishTank { get; set; }

        public bool? IsObligatory { get; set; }
        public bool? IsSolution { get; set; }
    }
}
