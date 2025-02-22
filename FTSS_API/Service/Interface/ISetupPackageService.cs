﻿
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SetupPackage;

namespace FTSS_API.Service.Interface
{
    public interface ISetupPackageService
    {
        Task<ApiResponse> AddSetupPackage(List<Guid> productids, AddSetupPackageRequest request);
        Task<ApiResponse> GetListSetupPackage(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> GetListSetupPackageAllUser(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> GetListSetupPackageShop(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> GetSetUpById(Guid id);
        Task<ApiResponse> RemoveSetupPackage(Guid id);
        Task<ApiResponse> UpdateSetupPackage(Guid id, AddSetupPackageRequest request, List<Guid> productIds);
    }
}
