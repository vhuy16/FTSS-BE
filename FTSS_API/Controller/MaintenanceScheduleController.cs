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

namespace FTSS_API.Controller
{
    public class MaintenanceScheduleController : BaseController<MaintenanceScheduleController>
    {
        private readonly IMaintenanceScheduleService _maintenanceScheduleService;
        public MaintenanceScheduleController(ILogger<MaintenanceScheduleController> logger, IMaintenanceScheduleService maintenanceScheduleService) : base(logger)
        {
            _maintenanceScheduleService = maintenanceScheduleService;
        }
        /// <summary>
        /// API phân việc cho kỹ thuật viên
        /// </summary>
        [HttpPost(ApiEndPointConstant.MaintenanceSchedule.AssigningTechnician)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AssigningTechnician([FromForm] Guid technicianid,[FromForm] Guid userid, [FromForm] AssigningTechnicianRequest request)
        {
            var response = await _maintenanceScheduleService.AssigningTechnician(technicianid, userid, request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API cancel task cho admin, manager, technician.
        /// </summary>
        [HttpPut(ApiEndPointConstant.MaintenanceSchedule.CancelTask)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CancelTask(Guid id)
        {
            var response = await _maintenanceScheduleService.CancelTask(id);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API lấy danh sách task cho admin, manager.
        /// </summary>
        [HttpGet(ApiEndPointConstant.MaintenanceSchedule.GetListTask)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListTask(
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] string? status,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _maintenanceScheduleService.GetListTask(pageNumber, pageSize, status, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.OrderMessage.OrderIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        /// <summary>
        /// API lấy danh sách task cho technician.
        /// </summary>
        [HttpGet(ApiEndPointConstant.MaintenanceSchedule.GetListTaskTech)]
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
            var response = await _maintenanceScheduleService.GetListTaskTech(pageNumber, pageSize, status, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.OrderMessage.OrderIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
    }
}
