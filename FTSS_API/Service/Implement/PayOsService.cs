using System.Net;
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

    public PayOsService(IOptions<PayOSSettings> settings, HttpClient client, IUnitOfWork<MyDbContext> unitOfWork,
        ILogger<PayOsService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor,
        IOrderService oderServicer)
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


    public async Task<Result<PaymentLinkResponse>> CreatePaymentUrlRegisterCreator(Guid? orderId, Guid? bookingId)
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

        decimal? totalPrice = 0;
        string description = "";

        if (orderId.HasValue)
        {
            // 🔹 Lấy thông tin Order
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

            // 🔹 Thêm chi tiết sản phẩm vào danh sách items
            foreach (var orderDetail in order.OrderDetails)
            {
                var itemData = new ItemData(orderDetail.Product.ProductName, (int)orderDetail.Quantity,
                    (int)orderDetail.Price);
                items.Add(itemData);
            }

            totalPrice = order.TotalPrice;
            description = $"Payment for Order #{order.Id}";
        }
        else if (bookingId.HasValue)
        {
            // 🔹 Lấy thông tin Booking
            var booking = await _unitOfWork.GetRepository<Booking>()
                .SingleOrDefaultAsync(
                    predicate: b => b.Id.Equals(bookingId),
                    include: b => b.Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.ServicePackage)
                );

            if (booking == null)
            {
                return Result<PaymentLinkResponse>.Failure("Booking not found");
            }

            // 🔹 Thêm chi tiết dịch vụ vào danh sách items
            foreach (var bookingDetail in booking.BookingDetails)
            {
                var itemData = new ItemData(bookingDetail.ServicePackage.ServiceName, 1,
                    (int)bookingDetail.ServicePackage.Price);
                items.Add(itemData);
            }

            totalPrice = booking.TotalPrice;
            description = $"Payment for Booking #{booking.Id}";
        }
        else
        {
            return Result<PaymentLinkResponse>.Failure("Either orderId or bookingId must be provided");
        }

        string buyerName = user.FullName;
        string buyerPhone = user.PhoneNumber;
        string buyerEmail = user.Email;

        Random random = new Random();
        long orderCode = (DateTime.Now.Ticks % 1000000000000000L) * 10 + random.Next(0, 1000);

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
                orderCode = orderCode,
                description = paymentResult.description
            };
            return Result<PaymentLinkResponse>.Success(paymentLinkResponse);
        }

        return Result<PaymentLinkResponse>.Failure("Failed to create payment link");
    }


    public Task<ApiResponse> HandlePaymentCallback(string paymentLinkId, long orderCode)
    {
        throw new NotImplementedException();
    }


    public async Task<Result> HandlePayOsWebhook(WebhookType webhookBody)
    {
        try
        {
            var existingPayment = await _unitOfWork.GetRepository<Payment>()
                .SingleOrDefaultAsync(predicate: p => p.OrderCode == webhookBody.data.orderCode);

            if (existingPayment == null)
            {
                _logger.LogError("Payment not found for orderCode: {OrderCode}", webhookBody.data.orderCode);
                return Result.Failure("Payment not found");
            }

            if (webhookBody.data.code == "00" && webhookBody.success)
            {
                if (existingPayment.Order.Id != null)
                {
                    await HandleSuccessfulPayment(existingPayment);
                }
                else if (existingPayment.BookingId != null)
                {
                    await HandleSuccessfulBookingPayment(existingPayment);
                }
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("Successfully processed webhook for orderCode: {OrderCode}",
                webhookBody.data.orderCode);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling webhook in service");
            return Result.Failure("An error occurred while handling webhook");
        }
    }

    private async Task HandleSuccessfulBookingPayment(Payment payment)
    {
        payment.PaymentStatus = PaymentStatusEnum.Completed.ToString();
        payment.PaymentDate = DateTime.UtcNow;

        var booking = await _unitOfWork.GetRepository<Booking>()
            .SingleOrDefaultAsync(predicate:
                b => b.Id == payment.BookingId);

        if (booking == null)
            throw new InvalidOperationException("Booking not found.");

        booking.Status = BookingStatusEnum.PAID.GetDescriptionFromEnum();


        _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
    }

    public async Task<ApiResponse> HandleFailedBookingPayment(Payment payment)
    {
        if (payment == null)
        {
            return new ApiResponse
            {
                data = null,
                message = "Payment could not be found",
                status = StatusCodes.Status404NotFound.ToString()
            };
        }

        var booking = await _unitOfWork.GetRepository<Booking>()
            .SingleOrDefaultAsync(predicate: b => b.Id == payment.BookingId);

        if (booking == null)
        {
            return new ApiResponse
            {
                data = null,
                message = "Booking could not be found",
                status = StatusCodes.Status404NotFound.ToString()
            };
        }

        payment.PaymentStatus = PaymentStatusEnum.Cancelled.ToString();
        payment.PaymentDate = DateTime.UtcNow;
        booking.Status = BookingStatusEnum.NOTPAID.GetDescriptionFromEnum();

        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
        await _unitOfWork.CommitAsync();

        return new ApiResponse
        {
            data = payment,
            message = "Booking payment could not be completed",
            status = StatusCodes.Status200OK.ToString()
        };
    }

    private async Task HandleSuccessfulPayment(Payment payment)
    {
        payment.PaymentStatus = PaymentStatusEnum.Completed.ToString();
        payment.PaymentDate = DateTime.UtcNow;

        var order = await _unitOfWork.GetRepository<Order>()
            .SingleOrDefaultAsync(
                predicate: o => o.Id == payment.OrderId,
                include: x => x.Include(x => x.OrderDetails).Include(u => u.User)
            );

        if (order == null || order.User == null)
            throw new InvalidOperationException("Order or User not found.");

        var userId = order.User.Id;
        var cart = await _unitOfWork.GetRepository<Cart>()
            .SingleOrDefaultAsync(predicate: c => c.UserId == userId);

        if (cart == null)
            throw new InvalidOperationException("Cart not found.");

        var productIds = order.OrderDetails.Select(od => od.ProductId).ToList();
        var products = await _unitOfWork.GetRepository<Product>()
            .GetListAsync(predicate: p => productIds.Contains(p.Id));
        var cartItems = await _unitOfWork.GetRepository<CartItem>()
            .GetListAsync(predicate: ci => ci.CartId == cart.Id && productIds.Contains(ci.ProductId));

        foreach (var od in order.OrderDetails)
        {
            var product = products.FirstOrDefault(p => p.Id == od.ProductId);
            if (product == null) continue;

            product.Quantity -= od.Quantity;
            _unitOfWork.GetRepository<Product>().UpdateAsync(product);

            var cartItem = cartItems.FirstOrDefault(ci => ci.ProductId == od.ProductId);
            if (cartItem != null)
            {
                _unitOfWork.GetRepository<CartItem>().DeleteAsync(cartItem);
            }
        }


        order.ModifyDate = DateTime.UtcNow;
        _unitOfWork.GetRepository<Order>().UpdateAsync(order);

        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
    }

    public async Task<ApiResponse> HandleFailedPayment(Guid orderCode)
    {
        var payment = await _unitOfWork.GetRepository<Payment>()
            .SingleOrDefaultAsync(predicate: p => p.Id.Equals(orderCode));
        if (payment == null)
        {
            return new ApiResponse()
            {
                data = null,
                message = "Payment could not be found",
                status = StatusCodes.Status404NotFound.ToString()
            };
        }

        if (payment.OrderId != null)
        {
            var order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: o => o.Id == payment.OrderId);
            if (order == null)
            {
                return new ApiResponse()
                {
                    data = null,
                    message = "Order could not be found",
                    status = StatusCodes.Status404NotFound.ToString()
                };
            }

            order.Status = OrderStatus.CANCELLED.GetDescriptionFromEnum();
        }


        else
        {
            if (payment.BookingId != null)
            {
                await HandleFailedBookingPayment(payment);
            }
        }

        payment.PaymentStatus = PaymentStatusEnum.Cancelled.ToString();
        payment.PaymentDate = DateTime.UtcNow;

        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        await _unitOfWork.CommitAsync();
        return new ApiResponse()
        {
            data = payment,
            message = "Payment could not be completed",
            status = StatusCodes.Status200OK.ToString()
        };
    }

    private bool ValidateWebhookSignature(string requestBody, string signatureFromPayOs)
    {
        var secretKey = _payOSSettings.ChecksumKey;

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return computedSignature == signatureFromPayOs.ToLower();
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
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Console.WriteLine("Webhook URL is null or empty.");
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Invalid webhook URL: null or empty",
                    data = null
                };
            }

            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out Uri uriResult))
            {
                Console.WriteLine("Webhook URL is not a valid absolute URI.");
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Invalid webhook URL: invalid format",
                    data = null
                };
            }

            Console.WriteLine($"Calling confirmWebhook with URL: {webhookUrl}");
            string result = await _payOS.confirmWebhook(webhookUrl);

            Console.WriteLine($"confirmWebhook result: {result}");

            if (!string.IsNullOrEmpty(result))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Webhook confirmed successfully.",
                    data = result
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
            Console.WriteLine($"Error confirming webhook: {ex.Message}, Stack Trace: {ex.StackTrace}");

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
        public string description { get; set; }
    }

    private bool SecureCompare(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b)
        );
    }
}