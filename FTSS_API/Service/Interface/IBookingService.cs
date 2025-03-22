using FTSS_API.Payload;
using FTSS_API.Payload.Request.Book;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Service.Interface
{
    public interface IBookingService
    {
        Task<ApiResponse> AssigningTechnician(AssigningTechnicianRequest request);
        Task<ApiResponse> AssigningTechnicianBooking(AssignTechBookingRequest request);
        Task<ApiResponse> BookingSchedule(BookingScheduleRequest request);
        Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status, bool? isAscending, bool? isAssigned);
        Task<ApiResponse> GetListMissionForManager(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTech(GetListTechRequest request);
        Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> UpdateStatusMission(Guid id, string status);
    }
}
