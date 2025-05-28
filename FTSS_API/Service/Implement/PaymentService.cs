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
        IHttpContextAccessor httpContextAccessor, IPayOSService payOsService, IEmailSender emailSender,
        IVnPayService vnPayService) : base(
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
            order = await _unitOfWork.Context.Set<Order>()
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);
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

        // Handle free booking case
        if (paymentMethod == PaymenMethodEnum.FREE.GetDescriptionFromEnum())
        {
            amountToPay = 0; // Set payment amount to 0 for FREE booking
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
            // Handle payment URL creation for other payment methods
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
                // COD is not supported for bookings
                if (booking != null)
                {
                    return new ApiResponse
                    {
                        data = string.Empty,
                        message = "COD is not supported for bookings",
                        status = StatusCodes.Status400BadRequest.ToString(),
                    };
                }

                // Deduct product quantities for COD
                if (order != null && order.OrderDetails != null)
                {
                    foreach (var orderDetail in order.OrderDetails)
                    {
                        var product = await _unitOfWork.Context.Set<Product>()
                            .FirstOrDefaultAsync(p => p.Id == orderDetail.ProductId);
                        if (product == null)
                        {
                            return new ApiResponse
                            {
                                data = string.Empty,
                                message = $"Product with ID {orderDetail.ProductId} not found",
                                status = StatusCodes.Status404NotFound.ToString(),
                            };
                        }

                        if (product.Quantity < orderDetail.Quantity)
                        {
                            return new ApiResponse
                            {
                                data = string.Empty,
                                message = $"Insufficient quantity for product {product.ProductName}",
                                status = StatusCodes.Status400BadRequest.ToString(),
                            };
                        }

                        product.Quantity -= orderDetail.Quantity;
                        _unitOfWork.Context.Set<Product>().Update(product);
                    }
                }
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

        // Create new Payment
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

        // Insert Payment into database
        await _unitOfWork.GetRepository<Payment>().InsertAsync(newPayment);

        // Commit all changes to the database (including product quantity updates)
        await _unitOfWork.CommitAsync();

        // Retrieve the saved payment by OrderCode
        var savedPayment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
            predicate: p => p.OrderCode == orderCode
        );

        if (savedPayment == null)
        {
            return new ApiResponse { message = "Failed to retrieve saved payment" };
        }

        // Return payment information
        var responseData = new Dictionary<string, object>
        {
            ["PaymentId"] = savedPayment.Id,
            ["Amount"] = savedPayment.AmountPaid
        };

        // Only add PaymentURL if not a FREE booking
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

    public async Task<ApiResponse> UpdatePaymentStatus(Guid paymentId, string newStatus)
    {
        var payment = await _unitOfWork.GetRepository<Payment>()
            .SingleOrDefaultAsync(predicate: o => o.Id == paymentId);

        if (payment == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Payment not found",
                data = null
            };
        }

        if (newStatus == PaymentStatusEnum.Refunded.ToString())
        {
            if (payment.OrderId.HasValue && !payment.BookingId.HasValue)
            {
                // Case: Order-only
                var order = await _unitOfWork.GetRepository<Order>()
                    .SingleOrDefaultAsync(predicate: o => o.Id == payment.OrderId,
                        include: query => query.Include(o => o.User));

                if (order != null && order.User != null)
                {
                    string emailBody =
                        EmailTemplatesUtils.RefundedNotificationEmailTemplate(orderCode: order.OrderCode);
                    await _emailSender.SendRefundNotificationEmailAsync(order.User.Email, emailBody);
                }
            }
            else if (payment.BookingId.HasValue && !payment.OrderId.HasValue)
            {
                // Case: Booking-only
                var booking = await _unitOfWork.GetRepository<Booking>()
                    .SingleOrDefaultAsync(predicate: b => b.Id == payment.BookingId,
                        include: q => q.Include(b => b.User));

                if (booking != null && booking.User != null)
                {
                    string emailBody =
                        EmailTemplatesUtils.RefundedNotificationEmailTemplate(bookingCode: booking.BookingCode);
                    await _emailSender.SendRefundNotificationEmailAsync(booking.User.Email, emailBody);
                }
            }
        }

        payment.PaymentStatus = newStatus;

        _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
        await _unitOfWork.CommitAsync();

        var paymentDto = new PaymentDto
        {
            Id = payment.Id,
            PaymentStatus = payment.PaymentStatus,
            OrderId = payment.OrderId
        };

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Payment status updated successfully",
            data = paymentDto
        };
    }

    public async Task<ApiResponse> UpdateBankInfor(Guid paymentId, string bankNumber, string bankName,
        string bankHolder)
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

    public async Task<ApiResponse> CancelExpiredProcessingPayments()
    {
        try
        {
            // Get payments that are in Processing status and older than 30 minutes
            var thirtyMinutesAgo = DateTime.Now.AddMinutes(-30);
            var processingPayments = await _unitOfWork.GetRepository<Payment>()
                .GetListAsync(
                    predicate: p => p.PaymentStatus == PaymentStatusEnum.Processing.GetDescriptionFromEnum()
                                    && p.PaymentDate <= thirtyMinutesAgo,
                    include: query => query.Include(p => p.Order).ThenInclude(o => o.User)
                );

            if (!processingPayments.Any())
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "No expired processing payments found",
                    data = null
                };
            }

            // Update status to Cancelled and send notification emails
            foreach (var payment in processingPayments)
            {
                payment.PaymentStatus = PaymentStatusEnum.Cancelled.GetDescriptionFromEnum();
                _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);

                // Send cancellation email if order and user exist
                if (payment.Order?.User?.Email != null)
                {
                    string emailBody = EmailTemplatesUtils.CancellationNotificationEmailTemplate(
                        payment.Order.Id,
                        payment.Order.OrderCode
                    );
                    await _emailSender.SendRefundNotificationEmailAsync(
                        payment.Order.User.Email,
                        emailBody
                    );
                }
            }

            // Commit changes to database
            await _unitOfWork.CommitAsync();

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = $"{processingPayments.Count()} expired processing payments cancelled successfully",
                data = new
                {
                    CancelledCount = processingPayments.Count(),
                    CancelledPaymentIds = processingPayments.Select(p => p.Id).ToList()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cancelling expired processing payments");
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "An error occurred while processing the request",
                data = null
            };
        }
    }
}