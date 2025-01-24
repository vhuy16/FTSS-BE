using FTSS_API.Payload;
using FTSS_API.Payload.Pay;

namespace FTSS_API.Service.Implement;

public interface IPayOSService
{
    Task<ExtendedPaymentInfo> GetPaymentInfo(string paymentLinkId);
    Task<ApiResponse> CreatePaymentUrlRegisterCreator(Guid orderId);
    Task<ApiResponse> HandlePaymentCallback(string paymentLinkId, long orderCode);
}