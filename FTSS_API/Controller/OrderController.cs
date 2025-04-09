using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Service.Interface;
using FTSS_Model.Paginate;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller;

public class OrderController : BaseController<OrderController>

{
    private readonly IOrderService _orderService;

    public OrderController(ILogger<OrderController> logger, IOrderService orderService) : base(logger)
    {
        _orderService = orderService;
    }
    /// <summary>
    /// API tạo order
    /// </summary>
    [HttpPost(ApiEndPointConstant.Order.CreateNewOrder)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest createOrderRequest)
    {

        var createOrderResponse = await _orderService.CreateOrder(createOrderRequest);
        if (createOrderResponse.status == StatusCodes.Status400BadRequest.ToString())
        {
            return BadRequest(createOrderResponse);
        }

        if (createOrderResponse.status == StatusCodes.Status404NotFound.ToString())
        {
            return NotFound(createOrderResponse);
        }

        return CreatedAtAction(nameof(CreateOrder), createOrderResponse);
    }

    /// <summary>
    /// API lấy danh sách tất cả đơn hàng cho admin.
    /// </summary>
    [HttpGet(ApiEndPointConstant.Order.GetListOrder)]
    [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetListOrder([FromQuery] int? page, [FromQuery] int? size,
        [FromQuery] bool? isAscending, [FromQuery] string? orderCode)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;
        var response = await _orderService.GetListOrder(pageNumber, pageSize, isAscending, orderCode);
        if (response.status == StatusCodes.Status200OK.ToString())
        {
            return Ok(response);
        }
        else if (response.status == StatusCodes.Status401Unauthorized.ToString())
        {
            return Unauthorized(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }
    /// <summary>
    /// API cập nhật đơn hàng
    /// </summary>
    [HttpPut(ApiEndPointConstant.Order.UpdateOrder)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateOrder([FromRoute] Guid id, [FromBody] UpdateOrderRequest updateOrderRequest)
    {
        if (updateOrderRequest == null)
        {
            return BadRequest(new ApiResponse
            {
                data = null,
                message = "Invalid request data",
                status = StatusCodes.Status400BadRequest.ToString(),
            });
        }

        var response = await _orderService.UpdateOrder(id, updateOrderRequest);

        if (!int.TryParse(response.status, out int statusCode))
        {
            statusCode = StatusCodes.Status500InternalServerError; // Mặc định nếu parsing lỗi
        }

        return StatusCode(statusCode, response);
    }


    /// <summary>
    /// API lấy danh sách đơn hàng cho user.
    /// </summary>
    [HttpGet(ApiEndPointConstant.Order.GetALLOrder)]
    [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetALLOrder(
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] string? status,
        [FromQuery] bool? isAscending,
        [FromQuery] string? orderCode)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;
        var response = await _orderService.GetAllOrder(pageNumber, pageSize, status, orderCode, isAscending);
        
        if (response.status == StatusCodes.Status200OK.ToString())
        {
            return Ok(response);
        }
        else if (response.status == StatusCodes.Status401Unauthorized.ToString())
        {
            return Unauthorized(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
        
    }

    /// <summary>
    /// API lấy đơn hàng theo orderid.
    /// </summary>
    [HttpGet(ApiEndPointConstant.Order.GetOrderById)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetOrder([FromRoute] Guid id)
    {
        var response = await _orderService.GetOrderById(id);
        return StatusCode(int.Parse(response.status), response);
    }

    // [HttpPut(ApiEndPointConstant.Order.UpdateOrder)]
    // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    // [ProducesErrorResponseType(typeof(ProblemDetails))]
    // public async Task<IActionResult> UpdateOrder([FromRoute] Guid id, [FromQuery] OrderStatus? orderStatus, [FromQuery]ShipEnum? shipStatus)
    // {
    //     var response = await _orderService.UpdateOrder(id, orderStatus, shipStatus);
    //     return StatusCode(int.Parse(response.status), response);
    // }
    /// <summary>
    /// API xoá order
    /// </summary>
    [HttpDelete(ApiEndPointConstant.Order.CancelOrder)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid id)
    {
        var response = await _orderService.CancelOrder(id);
        return StatusCode(int.Parse(response.status), response);
    }
    
}


