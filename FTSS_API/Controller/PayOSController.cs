using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Pay;
using FTSS_API.Service.Implement;
using FTSS_API.Utils;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FTSS_API.Controller;

 public class PayOSController : BaseController<PayOSController>
 {
     private readonly IPayOSService _payOsService;

     public PayOSController(ILogger<PayOSController> logger, IPayOSService payOsService) : base(logger)
     {
         _payOsService = payOsService;
     }

     // // Endpoint to create a payment URL
     // // Endpoint to create a payment URL
     // [HttpPost(ApiEndPointConstant.PaymentOS.CreatePaymentUrl)]
     // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
     // [ProducesErrorResponseType(typeof(ProblemDetails))]
     // public async Task<IActionResult> CreatePaymentUrl([FromBody] Guid orderID)
     // {
     //     var result = await _payOsService.CreatePaymentUrlRegisterCreator(orderID);
     //     return StatusCode(int.Parse(result.GetDescriptionFromEnum()), result);
     // }

     // Endpoint to get payment details
     /// <summary>
     /// Lấy thông tin chi tiết của một liên kết thanh toán.
     /// </summary>
     /// <param name="paymentLinkId">ID của liên kết thanh toán cần truy xuất.</param>
     /// <returns>Trả về thông tin chi tiết của liên kết thanh toán (ExtendedPaymentInfo).</returns>
     /// <response code="200">Lấy thông tin thanh toán thành công.</response>
     /// <response code="404">Không tìm thấy liên kết thanh toán.</response>
     /// <response code="500">Lỗi hệ thống khi truy xuất thông tin thanh toán.</response>
     [HttpGet(ApiEndPointConstant.PaymentOS.GetPaymentInfo)]
     [ProducesResponseType(typeof(ExtendedPaymentInfo), StatusCodes.Status200OK)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> GetPaymentInfo([FromRoute] string paymentLinkId)
     {
         try
         {
             var result = await _payOsService.GetPaymentInfo(paymentLinkId);
             if (result == null)
             {
                 return Problem(MessageConstant.PaymentMessage.PaymentNotFound);
             }
             return Ok(result);
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Error retrieving payment information.");
             return Problem(MessageConstant.PaymentMessage.PaymentNotFound);
         }
     }
     //
     // [HttpGet("ReturnUrl")]
     // public async Task<IActionResult> ReturnUrl()
     // {
     //
     //     // Lấy các tham số từ query string
     //     string responseCode = Request.Query["code"].ToString();
     //     string id = Request.Query["id"].ToString();
     //     string cancel = Request.Query["cancel"].ToString();
     //     string status = Request.Query["status"].ToString();
     //     string orderCode = Request.Query["orderCode"];
     //
     //     if (responseCode == "00" && status == "PAID")
     //     {
     //         try
     //         {
     //
     //             var response = await _payOsService.HandlePaymentCallback(id, long.Parse(orderCode));
     //             return Redirect("https://www.mrc.vn/payment/callback?status=success");
     //
     //         }
     //         catch (Exception ex)
     //         {
     //             return Problem("Đã xảy ra lỗi: " + ex.Message);
     //         }
     //     }
     //     else if (status == "CANCELLED")
     //     {
     //         var response = await _payOsService.HandlePaymentCallback(id, long.Parse(orderCode));
     //         return Redirect("https://www.mrc.vn/payment/callback?status=failed");
     //     }
     //     else
     //     {
     //         return Redirect("https://www.mrc.vn/payment/callback?status=failed");
     //     }
     // }
     /// <summary>
     /// Xử lý webhook từ PayOS để cập nhật trạng thái thanh toán.
     /// </summary>
     /// <param name="payload">Dữ liệu webhook từ PayOS, bao gồm thông tin thanh toán và chữ ký.</param>
     /// <returns>Trả về trạng thái xử lý webhook.</returns>
     /// <response code="200">Webhook được xử lý thành công.</response>
     /// <response code="400">Dữ liệu webhook không hợp lệ hoặc xử lý thất bại.</response>
     /// <response code="500">Lỗi hệ thống khi xử lý webhook.</response>
     [HttpPost("webhook-uri")]
     public async Task<IActionResult> HandlePayOsWebhook([FromBody] WebhookType payload)
     {
         try
         {
             var signatureFromPayOs = payload.signature; // Lấy signature từ body
             var requestBody = JsonConvert.SerializeObject(payload);
             var result = await _payOsService.HandlePayOsWebhook(payload);
             if (result.IsSuccess)
             {
                 return Ok();
             }
             return BadRequest(result.ErrorMessage);
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "An error occurred while handling webhook in controller.");
             return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the webhook.");
         }
     }
     /// <summary>
     /// Xử lý trường hợp thanh toán bị hủy.
     /// </summary>
     /// <returns>Chuyển hướng đến URL xác nhận hủy thanh toán.</returns>
     /// <response code="302">Chuyển hướng đến URL xác nhận hủy thanh toán.</response>
     /// <response code="500">Lỗi hệ thống khi xử lý thanh toán bị hủy.</response>
     [HttpGet("cancleUrl")]
     public async Task<IActionResult> handleCanclePayment()
     {
         string  status = Request.Query["status"].ToString();
         string id = Request.Query["id"].ToString();
         string orderCode = Request.Query["orderCode"];
         if (status == "CANCELLED")
         {
             var response = await _payOsService.HandleFailedPayment(Guid.Parse(orderCode));
             return Redirect("https://ftss.id.vn/api/v1/cancleUrl");
         }
         return Redirect("https://ftss.id.vn/api/v1/cancleUrl");
     }
     /// <summary>
     /// Xử lý trường hợp thanh toán thành công.
     /// </summary>
     /// <returns>Chuyển hướng đến URL xác nhận thanh toán thành công.</returns>
     /// <response code="302">Chuyển hướng đến URL xác nhận thanh toán thành công.</response>
     /// <response code="500">Lỗi hệ thống khi xử lý thanh toán thành công.</response>
     [HttpGet("successUrl")]
     public async Task<IActionResult> handleSuccessPayment()
     {
         string  status = Request.Query["status"].ToString();
         string id = Request.Query["id"].ToString();
         string orderCode = Request.Query["orderCode"];
         if (status == "PAID")
         {
             var response = await _payOsService.HandleSuccessfulPayment(long.Parse(orderCode));
             return Redirect("https://ftss-fe.vercel.app/paymentSuccess");
         }
         return Redirect("https://ftss-fe.vercel.app/paymentSuccess");
     }
     /// <summary>
     /// Xác nhận URL webhook với PayOS.
     /// </summary>
     /// <returns>Trả về kết quả xác nhận webhook.</returns>
     /// <response code="200">Xác nhận webhook thành công.</response>
     /// <response code="500">Lỗi hệ thống khi xác nhận webhook.</response>
     [HttpPost("confirm-webhook")]
     public async Task<IActionResult> ConfirmWebhook()
     {
         var webhookLink = "https://ftss.id.vn/api/v1/webhook";
         var result = await _payOsService.ConfirmWebhook(webhookLink);
         return Ok(result);
     }
 }