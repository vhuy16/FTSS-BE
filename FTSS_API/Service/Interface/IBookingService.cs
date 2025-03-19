using FTSS_API.Payload;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Service.Interface
{
    public interface IBookingService
    {
        Task<ApiResponse> AssigningTechnician(Guid technicianid, Guid userid, AssigningTechnicianRequest request);
        Task<ApiResponse> CancelTask(Guid id);
        Task<ApiResponse> GetListTask(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending);
    }
}
