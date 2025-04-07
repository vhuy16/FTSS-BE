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
        Task<ApiResponse> GetBookingById(Guid bookingid);
        Task<ApiResponse> GetDateUnavailable();
        Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status, bool? isAscending, bool? isAssigned);
        Task<ApiResponse> GetListBookingForUser(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListMissionForManager(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTech(GetListTechRequest request);
        Task<ApiResponse> GetMissionById(Guid missionid);
        Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> UpdateBooking(Guid bookingid, UpdateBookingRequest request);
        Task<ApiResponse> UpdateMission(Guid missionid, UpdateMissionRequest request);
        Task<ApiResponse> UpdateStatusMission(Guid id, string status);
    }
}
