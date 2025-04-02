using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Payload.Response.Pay.Payment;
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

    public PaymentService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<PaymentService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor, IPayOSService payOsService, IVnPayService vnPayService) : base(
        unitOfWork, logger, mapper, httpContextAccessor)
    {
        _payOSService = payOsService;
        _vnPayService = vnPayService;
    }

    public async Task<ApiResponse> CreatePayment(CreatePaymentRequest request)
    {
        Order? order = null;
        Booking? booking = null;

        if (request.OrderId != null)
        {
            order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: o => o.Id.Equals(request.OrderId)
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

        if (amountToPay <= 0)
        {
            return new ApiResponse
            {
                data = string.Empty,
                message = "No payment required",
                status = StatusCodes.Status400BadRequest.ToString(),
            };
        }

        // Xử lý tạo Payment URL dựa vào phương thức thanh toán
        string paymentUrl = string.Empty;
        string paymentMethod = request.PaymentMethod;
        long orderCode = DateTime.Now.Ticks % 1000000000000000L * 10 + new Random().Next(0, 1000);

        if (paymentMethod == PaymenMethodEnum.PayOs.GetDescriptionFromEnum())
        {
            var result = await _payOSService.CreatePaymentUrlRegisterCreator(request.OrderId, request.BookingId);

            if (result.IsSuccess && result.Value != null)
            {
                paymentUrl = result.Value.checkoutUrl;
            }
        }
        else if (paymentMethod == PaymenMethodEnum.VnPay.GetDescriptionFromEnum())
        {
            paymentUrl = await _vnPayService.CreatePaymentUrl(request.OrderId, request.BookingId);
        }


        if (order == null && booking == null)
        {
            return new ApiResponse { message = "Order and Booking cannot be both null" };
        }

// Tạo mới Payment
        var newPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order?.Id,
            BookingId = booking?.Id,
            PaymentMethod = paymentMethod,
            AmountPaid = amountToPay, // Đảm bảo không bị null
            PaymentDate = DateTime.Now,
            PaymentStatus = PaymentStatusEnum.Processing.ToString(),
            OrderCode = orderCode
        };

// Chèn vào database
        await _unitOfWork.GetRepository<Payment>().InsertAsync(newPayment);
        await _unitOfWork.CommitAsync();

// 🔹 Lấy lại payment vừa lưu bằng OrderCode hoặc Id
        var savedPayment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
            predicate: p => p.OrderCode == orderCode
        );

        if (savedPayment == null)
        {
            return new ApiResponse { message = "Failed to retrieve saved payment" };
        }

// Trả về thông tin payment vừa lưu
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Payment successful",
            data = new Dictionary<string, object>
            {
                ["PaymentId"] = savedPayment.Id,
                ["PaymentURL"] = paymentUrl, // Đảm bảo luôn có key này
                ["Amount"] = savedPayment.AmountPaid
            }
        };
    }

    public async Task<ApiResponse> UpdatePaymentStatus(Guid PaymentId, string newStatus)
    {
        var payment = await _unitOfWork.GetRepository<Payment>()
            .SingleOrDefaultAsync(predicate: o => o.Id == PaymentId);
        if (payment == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Order not found",
                data = null
            };
        }

        payment.Status = newStatus;
        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        await _unitOfWork.CommitAsync();

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Order status updated successfully",
            data = payment
        };
    }

    public async Task<ApiResponse> UpdateBankInfor(Guid paymentId, long? bankNumber, string bankName, string bankHolder)
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

        payment.BankNumber = bankNumber;
        payment.BankName = bankName;
        payment.BankHolder = bankHolder;
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