using FTSS_API.Payload;
using FTSS_API.Payload.Request.IssueCategory;
using FTSS_API.Payload.Response.IssueCategory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FTSS_API.Service.Interface
{
    public interface IIssueCategoryService
    {
        /// <summary>
        /// Tạo danh mục sự cố mới
        /// </summary>
        Task<ApiResponse> CreateIssueCategory(AddUpdateIssueCategoryRequest request);

        /// <summary>
        /// Lấy danh sách tất cả danh mục sự cố
        /// </summary>
        Task<ApiResponse> GetAllIssueCategories();

        /// <summary>
        /// Lấy danh mục sự cố theo ID
        /// </summary>
        Task<ApiResponse> GetIssueCategory(Guid id);

        /// <summary>
        /// Cập nhật danh mục sự cố
        /// </summary>
        Task<ApiResponse> UpdateIssueCategory(Guid id, AddUpdateIssueCategoryRequest request);

        /// <summary>
        /// Xóa danh mục sự cố
        /// </summary>
        Task<ApiResponse> DeleteIssueCategory(Guid id);
    }
}