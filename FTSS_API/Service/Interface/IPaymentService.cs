using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay;

namespace FTSS_API.Service.Interface;

public interface IPaymentService
{
    Task<ApiResponse> CreatePayment(CreatePaymentRequest request);
}