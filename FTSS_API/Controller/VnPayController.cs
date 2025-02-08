using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
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
    [HttpGet("callback")]
    public async Task<IActionResult> VnPayCallBack()
    {
        try
        {
            PaymentUltils.PayLib vnpay = new PaymentUltils.PayLib();
        
            // Lấy orderId từ query string VNPay gửi về
            string txnRef = vnpay.GetResponseData("vnp_TxnRef");
            Guid orderId = Guid.ParseExact(txnRef, "N");
            

            // Lấy trạng thái giao dịch
            string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");

            // Gọi service để xử lý callback
            var response = await _vnPayService.HandleCallBack(vnp_TransactionStatus, orderId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"VNPay Callback Error: {ex.Message}");
            return StatusCode(500, new ApiResponse
            {
                status = "500",
                message = "Lỗi xử lý callback VNPay",
                data = false
            });
        }
    }


}