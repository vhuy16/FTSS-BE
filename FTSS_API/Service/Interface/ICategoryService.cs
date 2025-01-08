﻿namespace FTSS_API.Service.Interface
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
        Task<ApiResponse> CreateCategory(CategoryRequest request);
        Task<ApiResponse> GetAllCategory(int page, int size, string searchName, bool? isAscending);
        Task<ApiResponse> GetCategory(Guid id);
        Task<ApiResponse> UpdateCategory(Guid id, CategoryRequest request);
        Task<ApiResponse> DeleteCategory(Guid id);
    }
}