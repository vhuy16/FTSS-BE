﻿using AutoMapper;
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
using Supabase.Gotrue;

namespace FTSS_API.Service.Implement;

public class PaymentService : BaseService<PaymentService>, IPaymentService
{
    private readonly IPayOSService _payOSService;
    private readonly IVnPayService _vnPayService;
        
    public PaymentService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<PaymentService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IPayOSService payOsService, IVnPayService vnPayService) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _payOSService = payOsService;
        _vnPayService = vnPayService;
    }

   public async Task<ApiResponse> CreatePayment(CreatePaymentRequest request)
{
    var order = await _unitOfWork.GetRepository<Order>()
        .SingleOrDefaultAsync(
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

    if (request.PaymentMethod == PaymenMethodEnum.PayOs.GetDescriptionFromEnum())
    {
        var result = await _payOSService.CreatePaymentUrlRegisterCreator(request.OrderId);
        
        if (result.IsSuccess && result.Value != null)
        {
            var paymentLinkResponse = result.Value;
            var createPaymentResponse = new CreatePaymentResponse
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = PaymenMethodEnum.PayOs.GetDescriptionFromEnum(),
                AmoundPaid = order.TotalPrice,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatusEnum.Processing.ToString(),
                PaymentURL = paymentLinkResponse.checkoutUrl,
                PaymentCode = paymentLinkResponse.orderCode,
                Description = paymentLinkResponse.description,
                
               
            };
        
            // Save payment to the database
            var payment = new Payment
            {
                Id = createPaymentResponse.Id,
                OrderId = createPaymentResponse.OrderId,
                PaymentMethod = createPaymentResponse.PaymentMethod,
                AmountPaid = createPaymentResponse.AmoundPaid,
                PaymentDate = createPaymentResponse.PaymentDate,
                PaymentStatus = PaymentStatusEnum.Processing.ToString(),
                OrderCode = createPaymentResponse.PaymentCode
            };
        
            await _unitOfWork.GetRepository<Payment>().InsertAsync(payment);
            await _unitOfWork.CommitAsync();
        
            return new ApiResponse
            {
                data = createPaymentResponse,
                message = "Payment created successfully",
                status = StatusCodes.Status200OK.ToString(),
            };
        }
        else
        {
            return new ApiResponse
            {
                data = string.Empty,
                message = "Failed to create payment URL",
                // status = response.status,
            };
        }
    }
    else if( request.PaymentMethod == PaymenMethodEnum.VnPay.GetDescriptionFromEnum())
    {
        var paymentUrl = await _vnPayService.CreatePaymentUrl(request.OrderId);
        Random random = new Random();
        long orderCode = (DateTime.Now.Ticks % 1000000000000000L) * 10 + random.Next(0, 1000);
        if (paymentUrl != null)
        {
          
            var createPaymentResponse = new CreatePaymentResponse
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = PaymenMethodEnum.VnPay.GetDescriptionFromEnum(),
                AmoundPaid = order.TotalPrice,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatusEnum.Processing.ToString(),
                PaymentURL = paymentUrl,
                PaymentCode = orderCode
               
            };
            var payment = new Payment
            {
                Id = createPaymentResponse.Id,
                OrderId = createPaymentResponse.OrderId,
                PaymentMethod = createPaymentResponse.PaymentMethod,
                AmountPaid = createPaymentResponse.AmoundPaid,
                PaymentDate = createPaymentResponse.PaymentDate,
                PaymentStatus = PaymentStatusEnum.Processing.ToString(),
                OrderCode = createPaymentResponse.PaymentCode
            };
        
            await _unitOfWork.GetRepository<Payment>().InsertAsync(payment);
            await _unitOfWork.CommitAsync();
            return new ApiResponse()
            {
                data = createPaymentResponse,
                message = "Payment created successfully",
                status = StatusCodes.Status200OK.ToString(),
            };
            
        }
        else
        {
            return new ApiResponse
            {
                data = string.Empty,
                message = "Failed to create payment URL",
                // status = response.status,
            };
        }    }
    return new ApiResponse
    {
        data = string.Empty,
        message = "Invalid payment method",
        status = StatusCodes.Status400BadRequest.ToString(),
    };
}

public async Task<ApiResponse> UpdatePaymentStatus(Guid PaymentId, string newStatus)
{
    var payment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(predicate: o => o.Id == PaymentId);
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