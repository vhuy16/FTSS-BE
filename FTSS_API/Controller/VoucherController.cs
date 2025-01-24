using FTSS_API.Constant;
using FTSS_API.Payload.Request.SubCategory;
using FTSS_API.Payload;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FTSS_API.Service.Interface;
using FTSS_API.Payload.Request.Product;
using FTSS_API.Payload.Request.Voucher;
using FTSS_API.Service.Implement;
using FTSS_Model.Paginate;

namespace FTSS_API.Controller
{
    [ApiController]
    [Route(ApiEndPointConstant.Voucher.VoucherEndPoint)]
    public class VoucherController : BaseController<VoucherController>
    {
        private readonly IVoucherService _voucherService;
        public VoucherController(ILogger<VoucherController> logger, IVoucherService voucherService) : base(logger)
        {
            _voucherService = voucherService;
        }
        /// <summary>
        /// API tạo mới Voucher.
        /// </summary>
        [HttpPost(ApiEndPointConstant.Voucher.AddVoucher)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AddVoucher([FromBody] VoucherRequest voucherRequest)
        {
            if (voucherRequest == null)
            {
                return BadRequest("Product request cannot be null.");
            }

            var response = await _voucherService.AddVoucher(voucherRequest);

            if (response.status == StatusCodes.Status201Created.ToString())
            {
                return CreatedAtAction(nameof(AddVoucher), new { id = response.data }, response);
            }
            else
            {
                return StatusCode(int.Parse(response.status), response);
            }
        }

        /// <summary>
        /// API lấy danh sách Voucher cho user.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Voucher.GetListVoucher)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListVoucher([FromRoute] 
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _voucherService.GetListVoucher(pageNumber, pageSize, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.VoucherMessage.VoucherIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        
        /// <summary>
        /// API lấy danh sách Voucher cho admin.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Voucher.GetAllVoucher)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllVoucher([FromRoute]
            [FromQuery] int? page,
            [FromQuery] int? size,
            [FromQuery] bool? isAscending = null,
            [FromQuery] string? status = null,
            [FromQuery] string? discountType = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _voucherService.GetAllVoucher(pageNumber, pageSize, isAscending, status, discountType);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.VoucherMessage.VoucherIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }

        /// <summary>
        /// API update voucher.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Voucher.UpdateVoucher)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateVoucher([FromRoute] Guid id,[FromForm] VoucherRequest voucherRequest)
        {
            var response = await _voucherService.UpdateVoucher(id, voucherRequest);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API delete voucher.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Voucher.DeleteVoucher)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteVoucher([FromRoute] Guid id)
        {
            var response = await _voucherService.DeleteVoucher(id);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
