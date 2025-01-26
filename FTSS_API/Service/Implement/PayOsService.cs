using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Pay;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FTSS_API.Service.Implement;

public class PayOsService : BaseService<PayOsService>, IPayOSService
{

    private readonly PayOS _payOS;
    private readonly PayOSSettings _payOSSettings;
    private readonly HttpClient _client;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderService _oderServicer;
    public PayOsService(IOptions<PayOSSettings> settings, HttpClient client, IUnitOfWork<MyDbContext> unitOfWork, ILogger<PayOsService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IOrderService oderServicer)
   : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _payOSSettings = settings.Value; // Lấy giá trị từ IOptions
        _payOS = new PayOS(_payOSSettings.ClientId, _payOSSettings.ApiKey, _payOSSettings.ChecksumKey);
        _client = client;
        _unitOfWork = unitOfWork;
        _oderServicer = oderServicer;
    }
    private string ComputeHmacSha256(string data, string checksumKey)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
    

     public async Task<Result<PaymentLinkResponse>> CreatePaymentUrlRegisterCreator(Guid orderId)
    {
        var items = new List<ItemData>();
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);

        if (!userId.HasValue)
        {
            return Result<PaymentLinkResponse>.Failure("User ID is null");
        }

        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
        if (user == null)
        {
            return Result<PaymentLinkResponse>.Failure("You need to login");
        }

        var order = await _unitOfWork.GetRepository<Order>()
            .SingleOrDefaultAsync(
                predicate: o => o.Id.Equals(orderId),
                include: o => o.Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
            );

        if (order == null)
        {
             return Result<PaymentLinkResponse>.Failure("Order not found");
        }

        var orderDetailIds = order.OrderDetails.Select(od => od.Id).ToList();

        foreach (var orderDetailId in orderDetailIds)
        {
            var orderDetail = await _unitOfWork.GetRepository<OrderDetail>().SingleOrDefaultAsync(
                predicate: o => o.Id.Equals(orderDetailId),
                include: od => od.Include(od => od.Product)
            );

            if (orderDetail != null && orderDetail.Product != null)
            {
                var price = orderDetail.Price;
                var productName = orderDetail.Product.ProductName;
                var quantity = orderDetail.Quantity;

                var itemData = new ItemData(productName, (int)quantity, (int)price);
                items.Add(itemData);
            }
        }

        string buyerName = user.FullName;
        string buyerPhone = user.PhoneNumber;
        string buyerEmail = user.Email;

        Random random = new Random();
        long orderCode = (DateTime.Now.Ticks % 1000000000000000L) * 10 + random.Next(0, 1000);
        var description = "VQRIO123";
        var totalPrice = order.TotalPrice;

        var signatureData = new Dictionary<string, object>
        {
            { "amount", totalPrice },
            { "cancelUrl", _payOSSettings.ReturnUrlFail },
            { "description", description },
            { "expiredAt", DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds() },
            { "orderCode", orderCode },
            { "returnUrl", _payOSSettings.ReturnUrl }
        };

        var sortedSignatureData = new SortedDictionary<string, object>(signatureData);
        var dataForSignature = string.Join("&", sortedSignatureData.Select(p => $"{p.Key}={p.Value}"));
        var signature = ComputeHmacSha256(dataForSignature, _payOSSettings.ChecksumKey);

        DateTimeOffset expiredAt = DateTimeOffset.Now.AddMinutes(10);
        var paymentData = new PaymentData(
            orderCode: orderCode,
            amount: (int)totalPrice,
            description: description,
            items: items,
            cancelUrl: _payOSSettings.ReturnUrlFail,
            returnUrl: _payOSSettings.ReturnUrl,
            signature: signature,
            buyerName: buyerName,
            buyerPhone: buyerPhone,
            buyerEmail: buyerEmail,
            buyerAddress: "HCM",
            expiredAt: (int)expiredAt.ToUnixTimeSeconds()
        );

        var paymentResult = await _payOS.createPaymentLink(paymentData);

       if (paymentResult != null)
        {
          var paymentLinkResponse = new PaymentLinkResponse()
            {
             checkoutUrl = paymentResult.checkoutUrl,
             orderCode = orderCode
            };
             return Result<PaymentLinkResponse>.Success(paymentLinkResponse);
        }
        return Result<PaymentLinkResponse>.Failure("Failed to create payment link");
    }
    public Task<ApiResponse> HandlePaymentCallback(string paymentLinkId, long orderCode)
    {
        
        throw new NotImplementedException();
    }


    public async Task<Result> HandlePayOsWebhook(WebhookType payload, string signatureFromPayOs, string requestBody)
    {
        try
        {
            _logger.LogInformation($"Webhook received: {JsonConvert.SerializeObject(payload)}");

            var signature = ComputeHmacSha256(requestBody, _payOSSettings.ChecksumKey);
            if (signature != signatureFromPayOs)
            {
                _logger.LogError("Invalid webhook signature");
                return Result.Failure("Invalid webhook signature");
            }

            if (payload?.data == null)
            {
                return Result.Failure("Invalid payload data");
            }

            var payment = payload.data;
            if (payment.orderCode == null)
            {
                return Result.Failure("Invalid order code data");
            }

            var getPayment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
                predicate: p => p.OrderCode.Equals(payment.orderCode));
            if (getPayment == null)
            {
                return Result.Failure("Payment is not found");
            }

            if (payload.success)
            {
                getPayment.PaymentStatus = PaymentStatusEnum.Completed.ToString();
                var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                    predicate: o => o.Id.Equals(getPayment.OrderId));
                order.Status = OrderStatus.PENDING_DELIVERY.GetDescriptionFromEnum();
                _unitOfWork.GetRepository<Payment>().UpdateAsync(getPayment);
                _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                await _unitOfWork.CommitAsync();
                return Result.Success();
            }
            else
            {
                getPayment.PaymentStatus = PaymentStatusEnum.Canceled.ToString();
                _unitOfWork.GetRepository<Payment>().UpdateAsync(getPayment);
                await _unitOfWork.CommitAsync();
                return Result.Success();
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling webhook in service.");
            return Result.Failure("An error occurred while handling webhook");
        }
    }

    public async Task<ExtendedPaymentInfo> GetPaymentInfo(string paymentLinkId)
    {
        try
        {
            var getUrl = $"https://api-merchant.payos.vn/v2/payment-requests/{paymentLinkId}";

            var request = new HttpRequestMessage(HttpMethod.Get, getUrl);
            request.Headers.Add("x-client-id", _payOSSettings.ClientId);
            request.Headers.Add("x-api-key", _payOSSettings.ApiKey);

            // Gửi yêu cầu HTTP
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<JObject>(responseContent);
            var paymentInfo = responseObject["data"].ToObject<ExtendedPaymentInfo>();

            return paymentInfo;
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while getting payment info.", ex);
        }
        return new ExtendedPaymentInfo();
    }

    public async Task<ApiResponse> ConfirmWebhook(string webhookUrl)
    {
        try
        {
            // Initialize PayOS with settings
            PayOS payOS = new PayOS(_payOSSettings.ClientId, _payOSSettings.ApiKey, _payOSSettings.ChecksumKey);

            // Confirm the webhook using the PayOS library
            string result = await payOS.confirmWebhook(webhookUrl);

            // Check the result and return an appropriate response
            if (!string.IsNullOrEmpty(result))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Webhook confirmed successfully.",
                    data = result // Include the result in the response
                };
            }
            else
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Webhook confirmation failed. The response was empty.",
                    data = null
                };
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            _logger.LogError(ex, "Error occurred while confirming the webhook.");

            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "An unexpected error occurred while confirming the webhook.",
                data = ex.Message
            };
        }
    }
    public class PaymentLinkResponse
    {
        public string checkoutUrl { get; set; }
        public long orderCode { get; set; }
    }
}