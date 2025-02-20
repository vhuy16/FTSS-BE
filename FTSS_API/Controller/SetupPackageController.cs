using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SetupPackage;
using FTSS_API.Payload.Request.SubCategory;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Paginate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller
{

    public class SetupPackageController : BaseController<SubCategoryController>
    {
        private readonly ISetupPackageService _setupPackageService;
        public SetupPackageController(ILogger<SubCategoryController> logger, ISetupPackageService setupPackageService) : base(logger)
        {
            _setupPackageService = setupPackageService;
        }
        /// <summary>
        /// API tạo mới setup cho mọi role
        /// </summary>
        [HttpPost(ApiEndPointConstant.SetupPackage.AddSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AddSetupPackage([FromForm] List<Guid> productids, [FromForm] AddSetupPackageRequest request)
        {
            var response = await _setupPackageService.AddSetupPackage(productids, request);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API delete setup cho mọi role.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.SetupPackage.RemoveSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> RemoveSetupPackage([FromRoute] Guid id)
        {
            var response = await _setupPackageService.RemoveSetupPackage(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy danh sách SetupPackage cho user.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetListSetupPackage)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListSetupPackage(
            [FromQuery] int? page = 1,
            [FromQuery] int? size = 10,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _setupPackageService.GetListSetupPackage(pageNumber, pageSize, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.VoucherMessage.VoucherIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }

        /// <summary>
        /// API lấy danh sách SetupPackage của shop cho admin, manager.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetListSetupPackageShop)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListSetupPackageShop(
            [FromQuery] int? page = 1,
            [FromQuery] int? size = 10,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _setupPackageService.GetListSetupPackageShop(pageNumber, pageSize, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.VoucherMessage.VoucherIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }

        /// <summary>
        /// API lấy danh sách SetupPackage của user cho admin, manager.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetListSetupPackageAllUser)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListSetupPackageAllUser(
            [FromQuery] int? page = 1,
            [FromQuery] int? size = 10,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _setupPackageService.GetListSetupPackageAllUser(pageNumber, pageSize, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.VoucherMessage.VoucherIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        /// <summary>
        /// API cập nhập Setup cho User
        /// </summary>
        [HttpPut(ApiEndPointConstant.SetupPackage.UpdateSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateSetupPackage([FromRoute] Guid id, [FromBody] AddSetupPackageRequest request, [FromQuery] List<Guid> productIds)
        {
            var response = await _setupPackageService.UpdateSetupPackage(id, request, productIds);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
