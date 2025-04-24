using FTSS_API.Constant;
using FTSS_API.Payload.Request.Category;
using FTSS_API.Payload;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FTSS_API.Payload.Request.ServicePackage;
using FTSS_Model.Paginate;

namespace FTSS_API.Controller;
public class ServicePackageController : BaseController<ServicePackageController>
{
    private readonly IServicePackageService _servicePackageService;
    public ServicePackageController(ILogger<ServicePackageController> logger, IServicePackageService servicePackageService) : base(logger)
    {
        _servicePackageService = servicePackageService;
    }

    /// <summary>
    /// API tạo mới gói dịch vụ.
    /// </summary>
    [HttpPost(ApiEndPointConstant.ServicePackage.AddServicePackage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> AddServicePackage([FromForm] ServicePackageRequest request)
    {
        var response = await _servicePackageService.AddServicePackage(request);
        return StatusCode(int.Parse(response.status), response);
    }
    /// <summary>
    /// API lấy danh sách service package.
    /// </summary>
    [HttpGet(ApiEndPointConstant.ServicePackage.GetServicePackage)]
    [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetServicePackage(
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] bool? isAscending = null)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;
        var response = await _servicePackageService.GetServicePackage(pageNumber, pageSize, isAscending);
        return StatusCode(int.Parse(response.status), response);
    }
    /// <summary>
    /// API cập nhật gói dịch vụ.
    /// </summary>
    [HttpPut(ApiEndPointConstant.ServicePackage.UpdateServicePackage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateServicePackage([FromRoute] Guid id, [FromForm] ServicePackageRequest request)
    {
        var response = await _servicePackageService.UpdateServicePackage(id, request);
        return StatusCode(int.Parse(response.status), response);
    }
    /// <summary>
    /// API kích hoạt gói dịch vụ.
    /// </summary>
    [HttpPut(ApiEndPointConstant.ServicePackage.EnableServicePackage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> EnableSubCategory([FromRoute] Guid id)
    {
        var response = await _servicePackageService.EnableServicePackage(id);
        return StatusCode(int.Parse(response.status), response);
    }
    /// <summary>
    /// API xóa gói dịch vụ.
    /// </summary>
    [HttpDelete(ApiEndPointConstant.ServicePackage.DeleteServicePackage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteSubCategory([FromRoute] Guid id)
    {
        var response = await _servicePackageService.DeleteServicePackage(id);
        return StatusCode(int.Parse(response.status), response);
    }
}

