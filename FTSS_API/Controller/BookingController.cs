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
        public async Task<IActionResult> AssigningTechnician([FromForm] Guid technicianid, [FromForm] Guid orderid, [FromForm] AssigningTechnicianRequest request)
        {
            var response = await _bookingService.AssigningTechnician(technicianid, orderid,  request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API phân việc cho kỹ thuật viên với những booking có sẵn
        /// </summary>
        [HttpPost(ApiEndPointConstant.Booking.AssigningTechnicianBooking)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AssigningTechnicianBooking([FromForm] Guid bookingid, [FromForm] Guid technicianid, [FromForm] AssignTechBookingRequest request)
        {
            var response = await _bookingService.AssigningTechnicianBooking(bookingid, technicianid, request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API đặt lịch cho khách hàng có order đã thanh toán.
        /// </summary>
        [HttpPost(ApiEndPointConstant.Booking.BookingSchedule)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> BookingSchedule([FromForm] List<Guid> serviceid,[FromForm] Guid orderid, [FromForm] BookingScheduleRequest request)
        {
            var response = await _bookingService.BookingSchedule(serviceid, orderid, request);
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
        public async Task<IActionResult> UpdateStatusMission(Guid id, string status)
        {
            var response = await _bookingService.UpdateStatusMission(id, status);
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
            [FromQuery] bool? isAscending = null,
            [FromQuery] bool? isAssigned = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _bookingService.GetListBookingForManager(pageNumber, pageSize, status, isAscending, isAssigned);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.MaintenanceScheduleMessage.MaintenanceScheduleIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
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
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.MaintenanceScheduleMessage.MissionIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        /// <summary>
        /// API lấy danh sách service package cho booking.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Booking.GetServicePackage)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetServicePackage(
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _bookingService.GetServicePackage(pageNumber, pageSize, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.MaintenanceScheduleMessage.ServiceIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        /// <summary>
        /// API lấy danh sách technician cho booking.
        /// </summary>
        /// 
        [HttpGet(ApiEndPointConstant.Booking.GetListTech)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListTech()
        {
            var response = await _bookingService.GetListTech();
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.MaintenanceScheduleMessage.TechIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        /// <summary>
        /// API lấy danh sách mission cho technician.
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
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.MaintenanceScheduleMessage.MissionIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
    }
}
