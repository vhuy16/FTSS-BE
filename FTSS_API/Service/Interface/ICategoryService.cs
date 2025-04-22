namespace FTSS_API.Service.Interface
{
    using FTSS_API.Controller;
    using FTSS_API.Payload;
    using FTSS_API.Payload.Request.Category;
    using FTSS_API.Payload.Response;
    using FTSS_API.Payload.Response.Category;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICategoryService
    {
        Task<ApiResponse> CreateCategory(CategoryRequest request, Supabase.Client client);
        Task<ApiResponse> GetAllCategory(int page, int size, string searchName, bool? isAscending);
        Task<ApiResponse> GetCategory(Guid id);
        Task<ApiResponse> UpdateCategory(Guid id, CategoryRequest request, Supabase.Client client);
        Task<ApiResponse> DeleteCategory(Guid id);
        Task<ApiResponse> GetListCategory(int v1, int v2, string? searchName, bool? isAscending);
        Task<ApiResponse> EnableCategory(Guid id);
    }
}