using FTSS_API.Payload.Request.Category;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SubCategory;

namespace FTSS_API.Service.Interface
{
    public interface ISubCategoryService
    {
        Task<ApiResponse> CreateSubCategory(SubCategoryRequest request);
        Task<ApiResponse> DeleteSubCategory(Guid id);
        Task<ApiResponse> GetAllSubCategories(int page, int size, string searchName, bool? isAscending);
        Task<ApiResponse> GetSubCategory(Guid id);
        Task<ApiResponse> UpdateSubCategory(Guid id, SubCategoryRequest request);
    }
}
