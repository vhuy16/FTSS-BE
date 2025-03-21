using FTSS_API.Payload;
using FTSS_API.Payload.Request.Book;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Service.Interface
{
    public interface IBookingService
    {
        Task<ApiResponse> AssigningTechnician(Guid technicianid, AssigningTechnicianRequest request);
        Task<ApiResponse> AssigningTechnicianBooking(Guid bookingid, Guid technicianid, AssignTechBookingRequest request);
        Task<ApiResponse> BookingSchedule(List<Guid> serviceid, Guid orderid, BookingScheduleRequest request);
        Task<ApiResponse> CancelTask(Guid id);
        Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status, bool? isAscending, bool? isAssigned);
        Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending);
        Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending);
    }
}
