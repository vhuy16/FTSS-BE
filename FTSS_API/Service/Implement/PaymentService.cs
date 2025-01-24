using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Payload.Response.Pay.Payment;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FTSS_API.Service.Implement;

public class PaymentService : BaseService<PaymentService>, IPaymentService
{
    private readonly IPayOSService _payOSService;
    public PaymentService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<PaymentService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IPayOSService payOsService) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _payOSService = payOsService;
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
        var response = await _payOSService.CreatePaymentUrlRegisterCreator(request.OrderId);
        if (response.status == StatusCodes.Status200OK.ToString())
        {
            var createPaymentResponse = new CreatePaymentResponse
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = PaymenMethodEnum.PayOs.GetDescriptionFromEnum(),
                AmoundPaid = order.TotalPrice,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatusEnum.Processing.ToString(),
                PaymentURL = response.data.ToString()
            };

            // Save payment to the database
            var payment = new Payment
            {
                Id = createPaymentResponse.Id,
                OrderId = createPaymentResponse.OrderId,
                PaymentMethod = createPaymentResponse.PaymentMethod,
                AmountPaid = createPaymentResponse.AmoundPaid,
                PaymentDate = createPaymentResponse.PaymentDate,
                PaymentStatus = PaymentStatusEnum.Completed.ToString()
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
                status = response.status,
            };
        }
    }

    return new ApiResponse
    {
        data = string.Empty,
        message = "Invalid payment method",
        status = StatusCodes.Status400BadRequest.ToString(),
    };
}


}