using FTSS_API.Payload;
using FTSS_API.Payload.Request.Book;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace FTSS_API.Service.Interface
{
    public interface IBookingService
    {
        Task<ApiResponse> AssigningTechnician(AssigningTechnicianRequest request);
        Task<ApiResponse> AssigningTechnicianBooking(AssignTechBookingRequest request);
        Task<ApiResponse> BookingSchedule(BookingScheduleRequest request);
        Task<ApiResponse> CancelBooking(Guid bookingid, CancelBookingRequest request);
        Task<ApiResponse> Confirm(Guid? orderid, Guid? bookingid);
        Task<ApiResponse> GetBookingById(Guid bookingid);
        Task<ApiResponse> GetDateUnavailable();
        Task<ApiResponse> GetHistoryOrder(Guid orderid);
        Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status, string? paymentstatus, string? bookingcode, bool? isAscending, bool? isAssigned);
        Task<ApiResponse> GetListBookingForUser(int pageNumber, int pageSize, string? status, string? paymentstatus, string? bookingcode, bool? isAscending);
        Task<ApiResponse> GetListMissionForManager(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetListTech(GetListTechRequest request);
        Task<ApiResponse> GetMissionById(Guid missionid);
        Task<ApiResponse> UpdateBooking(Guid bookingid, UpdateBookingRequest request);
        Task<ApiResponse> UpdateBookingStatus(Guid bookingid);
        Task<ApiResponse> UpdateMission(Guid missionId, UpdateMissionRequest request);

        Task<ApiResponse> UpdateStatusMission(Guid missionId, string status, Supabase.Client client,
            List<IFormFile>? ImageLinks, string? reason = null);
    }
}
