using System.Net;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay.VnPay;
using FTSS_API.Payload.Response.Pay;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FTSS_API.Service.Implement;

public class VnPayService : BaseService<VnPayService>, IVnPayService
{
    private readonly string _vnpTmnCode;
    private readonly string _vnpHashSecret;
    private readonly string _vnpReturnUrl;
    private readonly string _vnpUrl;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    private readonly VNPaySettings _vnpSettings;
    private readonly HttpClient _client;
    private readonly PaymentUltils.Utils utils;

    public VnPayService(IOptions<VNPaySettings> settings,
        HttpClient client,
        IUnitOfWork<MyDbContext> unitOfWork,
        ILogger<VnPayService> logger,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IOrderService oderServicer,
        PaymentUltils.Utils utils)
        : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _vnpSettings = settings.Value;
        _client = client;
        _orderService = oderServicer;

        _unitOfWork = unitOfWork;
        _vnpTmnCode = _vnpSettings.vnp_TmnCode;
        _vnpHashSecret = _vnpSettings.vnp_HashSecret;
        _vnpReturnUrl = _vnpSettings.vnp_ReturnUrl;
        _vnpUrl = _vnpSettings.vnp_Url;
        this.utils = utils;
    }

    public async Task<string> CreatePaymentUrl(Guid? orderId, Guid? bookingId)
    {
        try
        {
            Order? order = null;
            Booking? booking = null;
            decimal? totalPrice = 0;
            string referenceId = string.Empty;

            if (orderId.HasValue)
            {
                order = await _unitOfWork.GetRepository<Order>()
                    .SingleOrDefaultAsync(predicate: o => o.Id.Equals(orderId));
                if (order == null)
                {
                    throw new Exception("Order not found");
                }

                totalPrice = order.TotalPrice;
                referenceId = order.Id.ToString();
            }
            else if (bookingId.HasValue)
            {
                booking = await _unitOfWork.GetRepository<Booking>()
                    .SingleOrDefaultAsync(predicate: b => b.Id.Equals(bookingId));
                if (booking == null)
                {
                    throw new Exception("Booking not found");
                }

                totalPrice = booking.TotalPrice;
                referenceId = booking.Id.ToString();
            }
            else
            {
                throw new Exception("Order or Booking ID is required");
            }

            // Lấy địa chỉ IP của client
            string clientIPAddress = utils.GetIpAddress();

            PaymentUltils.PayLib pay = new PaymentUltils.PayLib();

            pay.AddRequestData("vnp_Version", PaymentUltils.PayLib.VERSION);
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", _vnpTmnCode);
            pay.AddRequestData("vnp_Amount", ((int)totalPrice * 100).ToString());
            pay.AddRequestData("vnp_BankCode", "");
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", clientIPAddress);
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", "Thanh toan: " + referenceId);
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", _vnpReturnUrl);
            pay.AddRequestData("vnp_TxnRef", referenceId);

            string paymentUrl = pay.CreateRequestUrl(_vnpUrl, _vnpHashSecret);
            return paymentUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating payment URL: {ex.Message}");
            throw new Exception("Failed to create payment URL", ex);
        }
    }

    public async Task<(ApiResponse, Order)> HandleCallBack(string status, Guid orderId)
    {
        try
        {
            var payment = await _unitOfWork.GetRepository<Payment>()
                .SingleOrDefaultAsync(predicate: p => p.OrderId.Equals(orderId));
            if (payment == null)
            {
                payment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
                    predicate: p => p.BookingId.Equals(orderId));
                if (payment == null)
                {
                    return (new ApiResponse()
                    {
                        data = null,
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Payment not found"
                    }, null);
                }
            }

            Order order = null;
            if (status == "00")
            {
                order = await HandleSuccessfulPayment(payment);
            }
            else
            {
                await HandleFailedPayment(payment);
            }

            await _unitOfWork.CommitAsync();
            return (new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Thanh toán thành công",
                data = true
            }, order);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<Order> HandleSuccessfulPayment(Payment payment)
    {
        payment.PaymentStatus = PaymentStatusEnum.Completed.ToString();
        payment.PaymentDate = DateTime.UtcNow;

        Order order = null;
        if (payment.OrderId.HasValue)
        {
            order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(
                    predicate: o => o.Id == payment.OrderId,
                    include: x => x.Include(o => o.OrderDetails).Include(u => u.User)
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

            order.Status = OrderStatus.PROCESSING.GetDescriptionFromEnum();
            order.ModifyDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
        }

        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        return order; // Trả về order
    }

    private async Task HandleFailedPayment(Payment payment)
    {
        payment.PaymentStatus = PaymentStatusEnum.Cancelled.ToString();
        payment.PaymentDate = DateTime.UtcNow;

        if (payment.OrderId.HasValue)
        {
            var order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: o => o.Id == payment.OrderId);

            if (order != null)
            {
                order.Status = OrderStatus.CANCELLED.ToString();
                _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            }
        }

        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
    }
 
        public async Task<ApiResponse> CancelPendingTransactions(TimeSpan timeout)
        {
            try
            {
                var canceledTransactions = new List<string>();
                var timeoutThreshold = DateTime.UtcNow.Add(-timeout);

                // Lấy danh sách các giao dịch Payment có trạng thái Processing
                var pendingPayments = await _unitOfWork.GetRepository<Payment>()
                    .GetListAsync(
                        predicate: p => p.PaymentStatus == PaymentStatusEnum.Processing.ToString(),
                        include: p => p
                            .Include(p => p.Order)
                            .Include(p => p.Booking)
                    );

                foreach (var payment in pendingPayments)
                {
                    try
                    {
                        // Xác định thời gian tạo (CreatedDate) từ Order hoặc Booking
                        DateTime? createdDate = null;
                        string txnRef = null;

                        if (payment.OrderId.HasValue && payment.Order != null)
                        {
                            createdDate = payment.Order.CreateDate;
                            txnRef = payment.OrderId.ToString();
                        }
                  
                        else
                        {
                            _logger.LogWarning($"Payment {payment.Id} không liên kết với Order hoặc Booking.");
                            continue;
                        }

                        // Kiểm tra nếu giao dịch đã vượt quá thời gian timeout
                        if (createdDate <= timeoutThreshold)
                        {
                            // Gọi API Query Transaction để kiểm tra trạng thái
                            var queryResult = await QueryTransactionStatus(txnRef, createdDate.Value);

                            // Nếu giao dịch không thành công (khác "00")
                            if (queryResult.vnp_TransactionStatus != "00")
                            {
                                // Cập nhật trạng thái Payment thành Cancelled
                                payment.PaymentStatus = PaymentStatusEnum.Cancelled.ToString();
                                payment.PaymentDate = DateTime.UtcNow;
                                _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);

                                // Cập nhật trạng thái Order (nếu có)
                                if (payment.OrderId.HasValue && payment.Order != null)
                                {
                                    payment.Order.Status = OrderStatus.CANCELLED.ToString();
                                    payment.Order.ModifyDate = DateTime.UtcNow;
                                    _unitOfWork.GetRepository<Order>().UpdateAsync(payment.Order);
                                }

                                // Cập nhật trạng thái Booking (nếu có)
                                if (payment.BookingId.HasValue && payment.Booking != null)
                                {
                                    payment.Booking.Status = "CANCELLED"; // Giả định Booking có Status
                                    _unitOfWork.GetRepository<Booking>().UpdateAsync(payment.Booking);
                                }

                                canceledTransactions.Add(txnRef);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error checking transaction {payment.OrderId ?? payment.BookingId}: {ex.Message}");
                        continue; // Tiếp tục xử lý các giao dịch khác
                    }
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = $"Hủy thành công {canceledTransactions.Count} giao dịch đang xử lý.",
                    data = canceledTransactions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling processing transactions: {ex.Message}");
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Lỗi khi hủy các giao dịch đang xử lý.",
                    data = null
                };
            }
        }

        private async Task<VnPayQueryResponse> QueryTransactionStatus(string txnRef, DateTime createDate)
        {
            try
            {
                var queryUrl = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
                var queryData = new Dictionary<string, string>
                {
                    { "vnp_TmnCode", _vnpTmnCode },
                    { "vnp_TxnRef", txnRef },
                    { "vnp_OrderInfo", "Query transaction status" },
                    { "vnp_TransDate", createDate.ToString("yyyyMMddHHmmss") },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_Version", "2.1.0" },
                    { "vnp_Command", "querydr" },
                    { "vnp_IpAddr", utils.GetIpAddress() }
                };

                // Tạo checksum
                var signData = string.Join("&", queryData.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"));
                var checksum = HmacSHA512(signData, _vnpHashSecret);
                queryData["vnp_SecureHash"] = checksum;

                // Gửi yêu cầu POST
                var content = new FormUrlEncodedContent(queryData);
                var response = await _client.PostAsync(queryUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<VnPayQueryResponse>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error querying VNPay transaction {txnRef}: {ex.Message}");
                throw;
            }
        }
        

        private string HmacSHA512(string input, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
     
}