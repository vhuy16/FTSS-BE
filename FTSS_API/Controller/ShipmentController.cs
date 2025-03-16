using System;
using System.Security.Cryptography;
using System.Text;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Shipment;
using FTSS_API.Service.Interface;
using FTSS_Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FTSS_API.Controller;

public class ShipmentController :  BaseController<ShipmentController>
{
    private readonly ILogger<ShipmentController> _logger;
    private readonly string _clientSecret;
    private readonly IShipmentService _shipmentService;
    
    public ShipmentController(ILogger<ShipmentController> logger, IConfiguration configuration, IShipmentService shipmentService) : base(logger)
    {
        _logger = logger;
        _clientSecret = configuration["Shipment:CLIENT_SECRET"] ?? throw new ArgumentNullException("CLIENT_SECRET not found in appsettings.json");
        _shipmentService = shipmentService;
    }

    private bool VerifyWebhook(string requestBody, string webhookHmac)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_clientSecret)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            string computedHmac = Convert.ToBase64String(hash);
            return computedHmac == webhookHmac;
        }
    }
    [HttpPost("listen")]
    public IActionResult ListenWebhook([FromBody] GoshipWebhookData webhookData)
    {
        try
        {
            // Serialize the received data to a string for HMAC verification
            string requestBody = System.Text.Json.JsonSerializer.Serialize(webhookData);
            string webhookHmac = Request.Headers["X-Goship-Hmac-SHA256"].ToString();

            if (string.IsNullOrEmpty(webhookHmac))
            {
                _logger.LogWarning("Webhook missing HMAC signature.");
                return Unauthorized("Missing HMAC signature.");
            }

            if (VerifyWebhook(requestBody, webhookHmac))
            {
                _logger.LogInformation("Webhook verified successfully.");

                // Process the webhook data
                _logger.LogInformation($"Received webhook data: Gcode={webhookData.Gcode}, OrderId={webhookData.OrderId}, Status={webhookData.Status}, Message={webhookData.Message}");

                // You can add your business logic here to handle the webhook data
                // For example, update the order status in your database

                return Ok("Webhook verified and processed");
            }
            else
            {
                _logger.LogWarning("Webhook verification failed.");
                return Unauthorized("Webhook verification failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing webhook: {ex.Message}");
            return StatusCode(500, "Internal server error.");
        }
    }
    /// <summary>
        /// API tạo shipment
        /// </summary>
        [HttpPost(ApiEndPointConstant.Shipment.CreateShipment)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateShipment([FromBody] ShipmentRequest shipmentRequest)
        {
            var response = await _shipmentService.CreateShipment(shipmentRequest);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy danh sách tất cả shipments
        /// </summary>
        [HttpGet(ApiEndPointConstant.Shipment.GetAllShipments)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllShipments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var response = await _shipmentService.GetAllShipments(page, pageSize, search);
            return Ok(response);
        }

        /// <summary>
        /// API lấy shipment theo ID
        /// </summary>
        [HttpGet(ApiEndPointConstant.Shipment.GetShipmentById)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetShipmentById([FromRoute] Guid id)
        {
            var response = await _shipmentService.GetShipmentById(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API cập nhật shipment
        /// </summary>
        [HttpPut(ApiEndPointConstant.Shipment.UpdateShipment)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateShipment([FromRoute] Guid id, [FromBody] ShipmentRequest shipmentRequest)
        {
            bool isUpdated = await _shipmentService.UpdateShipment(id, shipmentRequest);
            if (!isUpdated)
            {
                return BadRequest(new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Failed to update shipment",
                    data = null
                });
            }
            return Ok(new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Shipment updated successfully",
                data = null
            });
        }

        /// <summary>
        /// API xoá shipment
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Shipment.DeleteShipment)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteShipment([FromRoute] Guid id)
        {
            bool isDeleted = await _shipmentService.DeleteShipment(id);
            if (!isDeleted)
            {
                return BadRequest(new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Failed to delete shipment",
                    data = null
                });
            }
            return Ok(new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Shipment deleted successfully",
                data = null
            });
        }
    }
