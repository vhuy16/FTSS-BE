using System.Net;
using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay.VnPay;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
      public async Task<string> CreatePaymentUrl(Guid orderId)
      {
          try
          {

              var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(predicate: p => p.Id.Equals(orderId));
              string hostName = Dns.GetHostName();
              string clientIPAddress = utils.GetIpAddress();
              PaymentUltils.PayLib pay = new PaymentUltils.PayLib();

              pay.AddRequestData("vnp_Version", PaymentUltils.PayLib.VERSION);
              pay.AddRequestData("vnp_Command", "pay");
              pay.AddRequestData("vnp_TmnCode", _vnpTmnCode);
              pay.AddRequestData("vnp_Amount", ((int)order.TotalPrice * 100).ToString());
              pay.AddRequestData("vnp_BankCode", "");
              pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
              pay.AddRequestData("vnp_CurrCode", "VND");
              pay.AddRequestData("vnp_IpAddr", clientIPAddress);
              pay.AddRequestData("vnp_Locale", "vn");
              pay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + order.Id);
              pay.AddRequestData("vnp_OrderType", "other");
              pay.AddRequestData("vnp_ReturnUrl", _vnpReturnUrl);
              pay.AddRequestData("vnp_TxnRef", order.Id.ToString());

              string paymentUrl = pay.CreateRequestUrl(_vnpUrl, _vnpHashSecret);
              return paymentUrl;
          }
          catch (Exception ex)
          {
              _logger.LogError($"Error creating payment URL: {ex.Message}");
              throw new Exception("Failed to create payment URL", ex);
          }
      }

      public async Task<ApiResponse> HandleCallBack(string status, Guid orderId)
      {
          try
          {
              var payment = await _unitOfWork.GetRepository<Payment>()
                  .SingleOrDefaultAsync(predicate: p => p.OrderId.Equals(orderId));
              if (status == "00")
              {
                  await HandleSuccessfulPayment(payment);
              }
              else
              {
                  await HandleFailedPayment(payment);
              }

              await _unitOfWork.CommitAsync();
              return new ApiResponse()
              {
                  status = StatusCodes.Status200OK.ToString(),
                  message = "Thanh toán thành công",
                  data = true
              };
          }
          catch (Exception e)
          {
              Console.WriteLine(e);
              throw;
          }
      }

      private async Task HandleFailedPayment(Payment payment)
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

          order.Status = OrderStatus.PENDING_DELIVERY.GetDescriptionFromEnum();
          order.ModifyDate = DateTime.UtcNow;
          _unitOfWork.GetRepository<Order>().UpdateAsync(order);

          _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
      }

      private async Task HandleSuccessfulPayment(Payment payment)
      {
          payment.PaymentStatus = PaymentStatusEnum.Canceled.ToString();
          payment.PaymentDate = DateTime.UtcNow;
          _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
      }
  }