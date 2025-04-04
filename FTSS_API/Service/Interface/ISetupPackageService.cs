﻿
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SetupPackage;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace FTSS_API.Service.Interface
{
    public interface ISetupPackageService
    {
        Task<ApiResponse> AddSetupPackage(List<ProductSetupItem> productids, AddSetupPackageRequest request, Supabase.Client client);
        Task<ApiResponse> GetListSetupPackage(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> GetListSetupPackageAllUser(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> GetListSetupPackageAllShop(int pageNumber, int pageSize, bool? isAscending, double? minPrice, double? maxPrice);
        Task<ApiResponse> GetSetUpById(Guid id);
        Task<ApiResponse> RemoveSetupPackage(Guid id);
        Task<ApiResponse> CopySetupPackage(Guid setupPackageId);

        Task<ApiResponse> UpdateSetupPackage(Guid setupPackageId, List<ProductSetupItem> productIds,
            UpdateSetupPackageRequest request, Client client);
        // Task<ApiResponse> UpdateSetupPackage(
        //     List<ProductSetupItem> productIds,
        //     Guid setupPackageId,
        //     AddSetupPackageRequest request,
        //     Client client);
        // Task<ApiResponse> UpdateSetupPackage(Guid setupPackageId, List<ProductSetupItem> productids, AddSetupPackageRequest request, Supabase.Client client);
        Task<bool> enableSetupPackage(Guid setupPackageId);
    }
}
