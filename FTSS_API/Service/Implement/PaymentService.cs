using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Payload.Response.Pay.Payment;
using FTSS_API.Payload.Response.Payment;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Model.Paginate;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using Supabase.Gotrue;

namespace FTSS_API.Service.Implement;

public class PaymentService : BaseService<PaymentService>, IPaymentService
{
    private readonly IPayOSService _payOSService;
    private readonly IVnPayService _vnPayService;
    private readonly IEmailSender _emailSender;
    public PaymentService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<PaymentService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor, IPayOSService payOsService,IEmailSender emailSender, IVnPayService vnPayService) : base(
        unitOfWork, logger, mapper, httpContextAccessor)
    {
        _payOSService = payOsService;
        _vnPayService = vnPayService;
        _emailSender = emailSender;
    }

 public async Task<ApiResponse> CreatePayment(CreatePaymentRequest request)
{
    Order? order = null;
    Booking? booking = null;

    if (request.OrderId != null)
    {
        order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
            predicate: o => o.Id.Equals(request.OrderId),
            include: o => o.Include(o => o.OrderDetails).ThenInclude(od => od.Product) // Bao gồm OrderDetails và Product
        );

        if (order == null)
        {
            return new ApiResponse
            {
                data = string.Empty,
                message = "Order not found",
                status = StatusCodes.Status404NotFound.ToString(),
            };
        }
    }

    if (request.BookingId != null)
    {
        booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
            predicate: b => b.Id.Equals(request.BookingId)
        );

        if (booking == null)
        {
            return new ApiResponse
            {
                data = string.Empty,
                message = "Booking not found",
                status = StatusCodes.Status404NotFound.ToString(),
            };
        }
    }

    if (order == null && booking == null)
    {
        return new ApiResponse
        {
            data = string.Empty,
            message = "Invalid payment request",
            status = StatusCodes.Status400BadRequest.ToString(),
        };
    }

    decimal amountToPay = order?.TotalPrice ?? booking?.TotalPrice ?? 0;
    string paymentMethod = request.PaymentMethod;
    string paymentUrl = string.Empty;
    long orderCode = DateTime.Now.Ticks % 1000000000000000L * 10 + new Random().Next(0, 1000);

    // Xử lý trường hợp booking miễn phí
    if (paymentMethod == PaymenMethodEnum.FREE.GetDescriptionFromEnum())
    {
        amountToPay = 0; // Đặt số tiền thanh toán là 0 cho booking FREE
    }
    else if (amountToPay <= 0)
    {
        return new ApiResponse
        {
            data = string.Empty,
            message = "No payment required",
            status = StatusCodes.Status400BadRequest.ToString(),
        };
    }
    else
    {
        // Xử lý tạo Payment URL cho các phương thức thanh toán khác
        if (paymentMethod == PaymenMethodEnum.PayOs.GetDescriptionFromEnum())
        {
            var result = await _payOSService.CreatePaymentUrlRegisterCreator(request.OrderId, request.BookingId);
            if (result.IsSuccess && result.Value != null)
            {
                paymentUrl = result.Value.checkoutUrl;
                orderCode = result.Value.orderCode;
            }
        }
        else if (paymentMethod == PaymenMethodEnum.VnPay.GetDescriptionFromEnum())
        {
            paymentUrl = await _vnPayService.CreatePaymentUrl(request.OrderId, request.BookingId);
        }
        else if (paymentMethod == PaymenMethodEnum.COD.GetDescriptionFromEnum())
        {
            // COD không cần paymentUrl, chỉ cần tạo Payment với trạng thái Processing
            if (booking != null)
            {
                return new ApiResponse
                {
                    data = string.Empty,
                    message = "COD is not supported for bookings",
                    status = StatusCodes.Status400BadRequest.ToString(),
                };
            }
            // Có thể thêm kiểm tra nếu Order hỗ trợ COD (nếu cần)
        }
        else
        {
            return new ApiResponse
            {
                data = string.Empty,
                message = "Unsupported payment method",
                status = StatusCodes.Status400BadRequest.ToString(),
            };
        }
    }

    // Tạo mới Payment
    var newPayment = new Payment
    {
        Id = Guid.NewGuid(),
        OrderId = order?.Id,
        BookingId = booking?.Id,
        PaymentMethod = paymentMethod,
        AmountPaid = amountToPay,
        PaymentDate = DateTime.Now,
        PaymentStatus = paymentMethod == PaymenMethodEnum.FREE.GetDescriptionFromEnum() 
            ? PaymentStatusEnum.Completed.GetDescriptionFromEnum() 
            : PaymentStatusEnum.Processing.GetDescriptionFromEnum(),
        OrderCode = orderCode
    };

    // Chèn Payment vào database
    await _unitOfWork.GetRepository<Payment>().InsertAsync(newPayment);

    // Trừ số lượng sản phẩm nếu là COD
    if (paymentMethod == PaymenMethodEnum.COD.GetDescriptionFromEnum() && order != null)
    {
        foreach (var orderDetail in order.OrderDetails)
        {
            if (orderDetail.Product != null)
            {
                // Giả định Product có trường Quantity để quản lý số lượng tồn kho
                if (orderDetail.Product.Quantity < orderDetail.Quantity)
                {
                    return new ApiResponse
                    {
                        data = string.Empty,
                        message = $"Not enough stock for product {orderDetail.ProductId}",
                        status = StatusCodes.Status400BadRequest.ToString(),
                    };
                }

                // Trừ số lượng sản phẩm
                orderDetail.Product.Quantity -= orderDetail.Quantity;

                // Cập nhật Product trong database
                 _unitOfWork.GetRepository<Product>().UpdateAsync(orderDetail.Product);
            }
        }
    }

    // Commit tất cả thay đổi vào database
    await _unitOfWork.CommitAsync();

    // Lấy lại payment vừa lưu bằng OrderCode
    var savedPayment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
        predicate: p => p.OrderCode == orderCode
    );

    if (savedPayment == null)
    {
        return new ApiResponse { message = "Failed to retrieve saved payment" };
    }

    // Trả về thông tin payment vừa lưu
    var responseData = new Dictionary<string, object>
    {
        ["PaymentId"] = savedPayment.Id,
        ["Amount"] = savedPayment.AmountPaid
    };

    // Chỉ thêm PaymentURL nếu không phải booking FREE
    if (paymentMethod != PaymenMethodEnum.FREE.GetDescriptionFromEnum() && !string.IsNullOrEmpty(paymentUrl))
    {
        responseData["PaymentURL"] = paymentUrl;
    }

    return new ApiResponse
    {
        status = StatusCodes.Status200OK.ToString(),
        message = "Payment successful",
        data = responseData
    };
}
    public async Task<ApiResponse> GetPaymentsByStatus(string status, int page, int size)
    {
        var payments = await _unitOfWork.GetRepository<Payment>().GetPagingListAsync(
            predicate: p => p.PaymentStatus == status,
            selector: s => new CreatePaymentResponse()
            {
                Id = s.Id,
                OrderId = s.OrderId,
                PaymentMethod = s.PaymentMethod.ToString(),
                AmoundPaid = s.AmountPaid,
                PaymentStatus = s.PaymentStatus.ToString(),
                PaymentDate = s.PaymentDate,
            },
            page: page,
            size: size
        );

        int totalItems = payments.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = payments.Items.Any() ? "Payments retrieved successfully." : "No payments found.",
            data = new Paginate<CreatePaymentResponse>
            {
                Page = page,
                Size = size,
                Total = totalItems,
                TotalPages = totalPages,
                Items = payments.Items
            }
        };
    }

    public async Task<ApiResponse> UpdatePaymentStatus(Guid PaymentId, string newStatus)
    {
        var payment = await _unitOfWork.GetRepository<Payment>()
            .SingleOrDefaultAsync(predicate: o => o.Id == PaymentId);
        var order = await _unitOfWork.GetRepository<Order>()
            .SingleOrDefaultAsync(predicate: o => o.Id == payment.OrderId,
                include: query => query.Include(o => o.User));

        if (payment == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Order not found",
                data = null
            };
        }
        if (newStatus == PaymentStatusEnum.Refunded.ToString())
        {
            string emailBody = EmailTemplatesUtils.RefundedNotificationEmailTemplate(order.Id, order.OrderCode);
            var email = order.User.Email;
            await _emailSender.SendRefundNotificationEmailAsync(email, emailBody);
        }
        payment.PaymentStatus = newStatus;
       
        
        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        await _unitOfWork.CommitAsync();

        // Ánh xạ sang DTO
        var paymentDto = new PaymentDto
        {
            Id = payment.Id,
            PaymentStatus = payment.PaymentStatus,
            OrderId = payment.OrderId
        };

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Order status updated successfully",
            data = paymentDto
        };
    }
    public async Task<ApiResponse> UpdateBankInfor(Guid paymentId, string bankNumber, string bankName, string bankHolder)
    {
        var payment = await _unitOfWork.GetRepository<Payment>()
            .SingleOrDefaultAsync(predicate: p => p.Id == paymentId);
        if (payment == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Payment not found",
                data = null
            };
        }

        if (payment.PaymentStatus != PaymentStatusEnum.Completed.ToString())
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Payment status not completed",
                data = null
            };
        }
        payment.BankNumber = bankNumber;
        payment.BankName = bankName;
        payment.BankHolder = bankHolder;
        payment.PaymentStatus = PaymentStatusEnum.Refunding.ToString();
        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        await _unitOfWork.CommitAsync();

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Bank information updated successfully",
            data = payment
        };
    }

    public async Task<ApiResponse> GetPaymentById(Guid paymentId)
    {
        var payment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
            predicate: o => o.Id.Equals(paymentId)
        );

        if (payment == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Payment not found",
                data = null
            };
        }

        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            data = payment,
            message = "Payment retrieved successfully",
        };
    }

    public async Task<ApiResponse> GetPaymentByOrderId(Guid orderId)
    {
        var payment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
            predicate: o => o.Id.Equals(orderId)
        );
        if (payment == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Payment not found",
                data = null
            };
        }

        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            data = payment,
            message = "Payment retrieved successfully",
        };
    }

    public async Task<ApiResponse> GetPayments(int page, int size)
    {
        var payments = await _unitOfWork.GetRepository<Payment>().GetPagingListAsync(
            selector: s => new CreatePaymentResponse()
            {
                Id = s.Id,
                OrderId = s.OrderId,
                PaymentMethod = s.PaymentMethod.ToString(),
                AmoundPaid = s.AmountPaid,
                PaymentStatus = s.PaymentStatus.ToString(),
                PaymentDate = s.PaymentDate,
            },
            page: page,
            size: size
        );
        int totalItems = payments.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);
        if (payments == null || payments.Items.Count == 0)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Products retrieved successfully.",
                data = new Paginate<Payment>()
                {
                    Page = page,
                    Size = size,
                    Total = totalItems,
                    TotalPages = totalPages,
                    Items = new List<Payment>()
                }
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Products retrieved successfully.",
            data = payments
        };
    }
}