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
    
}