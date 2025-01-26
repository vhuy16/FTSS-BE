using FTSS_API.Constant;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace FTSS_API.Controller;

public class VnPayController : BaseController<VnPayController>
{
    private readonly IVnPayService _vnPayService;
    public VnPayController(ILogger<VnPayController> logger, IVnPayService vnPayService) : base(logger)
    {
        _vnPayService = vnPayService;
    }

    // Endpoint to create a VNPay payment URL
    [HttpPost(ApiEndPointConstant.VNPay.CreatePaymentUrl)]
    [ProducesResponseType(typeof(CreatePaymentResult), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> CreatePaymentUrl([FromQuery] Guid orderId)
    {
        try
        {
            var result = await _vnPayService.CreatePaymentUrl(orderId);
            if (string.IsNullOrEmpty(result))
            {
                return Problem(MessageConstant.PaymentMessage.CreatePaymentFail);
            }
            return Ok(new { PaymentUrl = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VNPay payment URL.");
            return Problem(MessageConstant.PaymentMessage.CreatePaymentFail);
        }
    }

}