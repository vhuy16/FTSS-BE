﻿using FTSS_API.Payload.Response.SubCategory;

namespace FTSS_API.Payload.Response.Category
{
    public class CategoryResponse
    {
        // ID của danh mục
        public Guid Id { get; set; }

        // Tên danh mục
        public string CategoryName { get; set; } = null!;

        // Mô tả danh mục (nếu có)
        public string? Description { get; set; }

        // Ngày tạo
        public DateTime? CreateDate { get; set; }

        // Ngày chỉnh sửa gần nhất
        public DateTime? ModifyDate { get; set; }
        public bool? IsDelete { get; set; }
        public bool? IsFishTank { get; set; }

        public bool? IsObligatory { get; set; }
        public bool? IsSolution { get; set; }
        public string LinkImage { get; set; } = null!;
        public List<SubCategoryResponse> SubCategories { get; set; } = new List<SubCategoryResponse>();


    }
}
