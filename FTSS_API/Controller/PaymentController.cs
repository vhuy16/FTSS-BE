using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller;

public class PaymentController : BaseController<PaymentController>
{
    private readonly IPaymentService _paymentService;
    public PaymentController(ILogger<PaymentController> logger, IPaymentService paymentService) : base(logger)
    {
        _paymentService = paymentService;
    }
    /// <summary>
    /// API tạo Payment.
    /// </summary>
    [HttpPost(ApiEndPointConstant.Payment.CreatePaymentUrl)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        // Validate the request object
        if (request == null || request.OrderId == Guid.Empty)
        {
            return BadRequest(new ApiResponse
            {
                data = string.Empty,
                message = "Invalid request or Order ID",
                status = StatusCodes.Status400BadRequest.ToString(),
            });
        }

        // Delegate the payment creation to the service
        var result = await _paymentService.CreatePayment(request);

        // Return the response based on the result
        if (result.status == StatusCodes.Status200OK.ToString())
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }
    /// <summary>
    /// API cập nhật trạng thái thanh toán.
    /// </summary>
    [HttpPut(ApiEndPointConstant.Payment.UpdatePaymentStatus)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePaymentStatus([FromBody]  String Status, [FromForm]  Guid PaymentId)
    {
      

        var result = await _paymentService.UpdatePaymentStatus(PaymentId, Status);

        return result.status == StatusCodes.Status200OK.ToString() ? Ok(result) : BadRequest(result);
    }
    [HttpPut(ApiEndPointConstant.Payment.UpdateBankInfor)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBankInfor(Guid paymentId, long? bankNumber, string bankName, string bankHolder)
    {
        var result = await _paymentService.UpdateBankInfor(paymentId, bankNumber, bankName, bankHolder);

        return result.status == StatusCodes.Status200OK.ToString() ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// API lấy Payment theo id
    /// </summary>
    [HttpGet(ApiEndPointConstant.Payment.GetPaymentById)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var result = await _paymentService.GetPaymentById(paymentId);
        return result.status == StatusCodes.Status200OK.ToString() ? Ok(result) : NotFound(result);
    }
    /// <summary>
    /// API lấy Payment theo id của order
    /// </summary>
    [HttpGet(ApiEndPointConstant.Payment.GetPaymentByOrderId)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByOrderId(Guid orderId)
    {
        var result = await _paymentService.GetPaymentByOrderId(orderId);
        return result.status == StatusCodes.Status200OK.ToString() ? Ok(result) : NotFound(result);
    }
    /// <summary>
    /// API lấy tất cả Payment cho admin
    /// </summary>
    [HttpGet(ApiEndPointConstant.Payment.GetPayments)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var result = await _paymentService.GetPayments(page, size);
        return Ok(result);
    }
    [HttpGet(ApiEndPointConstant.Payment.GetPaymentByStatus)]
    public async Task<IActionResult> GetPaymentsByStatus([FromQuery] string paymentStatus , [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var result = await _paymentService.GetPaymentsByStatus(paymentStatus, page, size);
        return Ok(result);
    }
}