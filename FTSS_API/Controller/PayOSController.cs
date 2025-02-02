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

     // Endpoint to create a payment URL
     // Endpoint to create a payment URL
     [HttpPost(ApiEndPointConstant.PaymentOS.CreatePaymentUrl)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> CreatePaymentUrl([FromBody] Guid orderID)
     {
         var result = await _payOsService.CreatePaymentUrlRegisterCreator(orderID);
         return StatusCode(int.Parse(result.GetDescriptionFromEnum()), result);
     }

     // Endpoint to get payment details
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
     [HttpPost("webhook-url")]
     public async Task<IActionResult> HandlePayOsWebhook([FromBody] WebhookType payload)
     {
         try
         {
             var signatureFromPayOs = HttpContext.Request.Headers["x-payos-signature"].ToString();
             var requestBody = JsonConvert.SerializeObject(payload);
             var result = await _payOsService.HandlePayOsWebhook(payload,signatureFromPayOs,requestBody);
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
     
     [HttpPost("confirm-webhook")]
     public async Task<IActionResult> ConfirmWebhook()
     {
         var webhookLink = "https://ftss.id.vn/api/v1/webhook";
         var result = await _payOsService.ConfirmWebhook(webhookLink);
         return Ok(result);
     }
 }