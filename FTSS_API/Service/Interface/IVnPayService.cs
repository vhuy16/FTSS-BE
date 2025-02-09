using FTSS_API.Payload;

namespace FTSS_API.Service.Interface;

public interface IVnPayService
{
    Task<string> CreatePaymentUrl(Guid orderId);
    Task<ApiResponse> HandleCallBack(string status, Guid orderId);
}