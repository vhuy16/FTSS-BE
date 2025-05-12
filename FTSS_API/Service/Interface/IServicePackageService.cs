using FTSS_API.Payload;
using FTSS_API.Payload.Request.Category;
using FTSS_API.Payload.Request.ServicePackage;
using Supabase;

namespace FTSS_API.Service.Interface
{
    public interface IServicePackageService
    {
        Task<ApiResponse> AddServicePackage(ServicePackageRequest request);

        Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> DeleteServicePackage(Guid id);
        Task<ApiResponse> EnableServicePackage(Guid id);
        Task<ApiResponse> UpdateServicePackage(Guid id, ServicePackageRequest request);
    }
}
