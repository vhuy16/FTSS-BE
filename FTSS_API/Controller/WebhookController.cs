using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly string _clientSecret = "e311924fb1e2a5e3094a89395adbf847dd21fd729588de55e84167a793eba23f"; // Thay bằng Client Secret của bạn
    [HttpPost]
    public IActionResult HandleWebhook()
    {
        try
        {
            // Đọc body của request
            using (var reader = new StreamReader(Request.Body))
            {
                var jsonBody = reader.ReadToEnd();
                var data = JsonConvert.DeserializeObject<dynamic>(jsonBody);

                // Xử lý dữ liệu webhook ở đây
                // Ví dụ: lấy orderCode từ dữ liệu webhook
                var orderCode = data?["orderCode"]?.ToString();
                Console.WriteLine($"Received webhook for order: {orderCode}");

                // Trả về response thành công
                return Ok(new
                {
                    code = "00",
                    message = "success"
                });
            }
        }
        catch (Exception ex)
        {
            // Log lỗi (nếu cần)
            Console.WriteLine($"Error processing webhook: {ex.Message}");

            // Trả về response với status code 200 ngay cả khi có lỗi
            return Ok(new
            {
                code = "00",
                message = "success"
            });
        }
    }
    private string ComputeSignature(string data, string clientSecret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}