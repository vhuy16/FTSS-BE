using FTSS_API.Constant;
using FTSS_API.Payload.Request.SetupPackage;
using FTSS_API.Payload;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using FTSS_API.Payload.Request.Category;
using FTSS_Model.Paginate;
using FTSS_API.Payload.Request.Book;
using Azure;
using System.Drawing;
using Supabase;

namespace FTSS_API.Controller
{
    public class BookingController : BaseController<BookingController>
    {
        private readonly IBookingService _bookingService;
        public BookingController(ILogger<BookingController> logger, IBookingService bookingService) : base(logger)
        {
            _bookingService = bookingService;
        }
        /// <summary>
        /// API phân việc cho kỹ thuật viên giao hàng và lắp đặt
        /// </summary>
        [HttpPost(ApiEndPointConstant.Booking.AssigningTechnician)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AssigningTechnician([FromBody] AssigningTechnicianRequest request)
        {
            var response = await _bookingService.AssigningTechnician(request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API phân việc cho kỹ thuật viên với những booking có sẵn
        /// </summary>
        [HttpPost(ApiEndPointConstant.Booking.AssigningTechnicianBooking)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AssigningTechnicianBooking([FromBody] AssignTechBookingRequest request)
        {
            var response = await _bookingService.AssigningTechnicianBooking(request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API đặt lịch cho khách hàng có order đã thanh toán.
        /// </summary>
        [HttpPost(ApiEndPointConstant.Booking.BookingSchedule)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> BookingSchedule([FromBody] BookingScheduleRequest request)
        {
            var response = await _bookingService.BookingSchedule(request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API update mission cho technician.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Booking.UpdateStatusMission)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateStatusMission(Guid id, string status, [FromForm] List<IFormFile>? ImageLinks, Client client,  string? reason = null )
        {
            var response = await _bookingService.UpdateStatusMission(id, status, client, ImageLinks, reason);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API report cho user.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Booking.Report)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> Report(Guid id, [FromForm] List<IFormFile>? ImageLinks, Client client, string? reason = null)
        {
            var response = await _bookingService.Report(id, client, ImageLinks, reason);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách booking cho manager.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetListBookingForManager)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListBookingForManager(
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] string? status,
            [FromQuery] string? paymentstatus,
            [FromQuery] string? bookingcode,
            [FromQuery] bool? isAscending = null,
            [FromQuery] bool? isAssigned = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _bookingService.GetListBookingForManager(pageNumber, pageSize, status, paymentstatus, bookingcode, isAscending, isAssigned);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách mission cho technician.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetListMissionTech)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListTaskTech(
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] string? status,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _bookingService.GetListTaskTech(pageNumber, pageSize, status, isAscending);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách technician cho manager.
        /// </summary>
        /// 
        [HttpPost(ApiEndPointConstant.Booking.GetListTech)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListTech([FromBody] GetListTechRequest request)
        {
            var response = await _bookingService.GetListTech(request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách mission cho manager.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetListMissionForManager)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListMissionForManager(
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] string? status,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _bookingService.GetListMissionForManager(pageNumber, pageSize, status, isAscending);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách ngày không thể book.
        /// </summary>
        /// 
        [HttpGet(ApiEndPointConstant.Booking.GetDateUnavailable)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetDateUnavailable()
        {
            var response = await _bookingService.GetDateUnavailable();
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách booking cho user.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetListBookingForUser)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListBookingForUser(
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] string? status,
            [FromQuery] string? paymentstatus,
            [FromQuery] string? bookingcode,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _bookingService.GetListBookingForUser(pageNumber, pageSize, status , paymentstatus,bookingcode, isAscending);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy booking detail.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetBookingById)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetBookingById(Guid bookingid)
        {
            var response = await _bookingService.GetBookingById(bookingid);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API cập nhật thông tin mission cho manager.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Booking.UpdateMission)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateMission(Guid missionid, [FromForm] UpdateMissionRequest request)
        {
            var response = await _bookingService.UpdateMission(missionid, request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy mission detail.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetMissionById)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetMissionById(Guid missionid)
        {
            var response = await _bookingService.GetMissionById(missionid);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API cập nhật thông tin booking cho user.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Booking.UpdateBooking)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateBooking(Guid bookingid, [FromForm] UpdateBookingRequest request)
        {
            var response = await _bookingService.UpdateBooking(bookingid, request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API cancel booking.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Booking.CancelBooking)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CancelBooking(Guid bookingid)
        {
            var response = await _bookingService.CancelBooking(bookingid);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lịch sử bảo trì của order.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetHistoryOrder)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetHistoryOrder(Guid orderid)
        {
            var response = await _bookingService.GetHistoryOrder(orderid);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API confirm cho user.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Booking.Confirm)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> Confirm(Guid? orderid,Guid? bookingid)
        {
            var response = await _bookingService.Confirm(orderid, bookingid);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
