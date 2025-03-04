﻿using System;
using System.Security.Cryptography;
using System.Text;
using FTSS_API.Payload.Request.Shipment;
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
    
    public ShipmentController(ILogger<ShipmentController> logger, IConfiguration configuration) : base(logger)
    {
        _logger = logger;
        _clientSecret = configuration["Shipment:CLIENT_SECRET"] ?? throw new ArgumentNullException("CLIENT_SECRET not found in appsettings.json");
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
}