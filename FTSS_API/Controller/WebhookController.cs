using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

[ApiController]
public class WebhookController : ControllerBase
{

    // [HttpPost("api/v1/webhook-url")]// Đặt route cụ thể cho action method
    // public IActionResult HandleWebhook()
    // {
    //     try
    //     {
    //         // Đọc body của request
    //         using (var reader = new StreamReader(Request.Body))
    //         {
    //             var jsonBody = reader.ReadToEnd();
    //             var data = JsonConvert.DeserializeObject<dynamic>(jsonBody);
    //
    //             // Xử lý dữ liệu webhook ở đây
    //             // Ví dụ: lấy orderCode từ dữ liệu webhook
    //             var orderCode = data?["orderCode"]?.ToString();
    //             Console.WriteLine($"Received webhook for order: {orderCode}");
    //
    //             // Trả về response thành công
    //             return Ok(new
    //             {
    //                 code = "00",
    //                 message = "success"
    //             });
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         // Log lỗi (nếu cần)
    //         Console.WriteLine($"Error processing webhook: {ex.Message}");
    //
    //         // Trả về response với status code 200 ngay cả khi có lỗi
    //         return Ok(new
    //         {
    //             code = "00",
    //             message = "success"
    //         });
    //     }
    // }
    // private string ComputeSignature(string data, string clientSecret)
    // {
    //     using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret)))
    //     {
    //         var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    //         return BitConverter.ToString(hash).Replace("-", "").ToLower();
    //     }
    // }
}
