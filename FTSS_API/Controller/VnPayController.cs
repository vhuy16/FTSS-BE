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
        // Lấy query string từ request
        var queryString = HttpContext.Request.Query;

        // Lấy giá trị của vnp_TxnRef từ query string
        if (!queryString.TryGetValue("vnp_TxnRef", out var txnRef))
        {
            return BadRequest(new ApiResponse
            {
                status = "400",
                message = "Thiếu tham số vnp_TxnRef trong phản hồi từ VNPay",
                data = false
            });
        }

        // Chuyển đổi txnRef sang Guid
        Guid orderId;
        try
        {
            orderId = new Guid(txnRef); // Sử dụng new Guid() để parse
        }
        catch (FormatException)
        {
            return BadRequest(new ApiResponse
            {
                status = "400",
                message = "Order ID không hợp lệ",
                data = false
            });
        }

        // Lấy trạng thái giao dịch
        if (!queryString.TryGetValue("vnp_TransactionStatus", out var vnp_TransactionStatus))
        {
            return BadRequest(new ApiResponse
            {
                status = "400",
                message = "Thiếu tham số vnp_TransactionStatus trong phản hồi từ VNPay",
                data = false
            });
        }

        // Gọi service để xử lý callback
        var response = await _vnPayService.HandleCallBack(vnp_TransactionStatus, orderId);

        // Kiểm tra trạng thái giao dịch và thực hiện redirect
        if (vnp_TransactionStatus == "00") // "00" là mã thành công của VNPay
        {
            // Chuyển hướng đến trang thành công
            return Redirect("https://ftss-fe.vercel.app/paymentSuccess");
        }
        else
        {
            // Chuyển hướng đến trang thất bại
            return Redirect("https://ftss-fe.vercel.app/paymentError");
        }
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