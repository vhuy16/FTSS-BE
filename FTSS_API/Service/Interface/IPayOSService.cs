using FTSS_API.Payload;
using FTSS_API.Payload.Pay;
using Net.payOS.Types;
using Newtonsoft.Json.Linq;

namespace FTSS_API.Service.Implement;

public interface IPayOSService
{
    Task<ExtendedPaymentInfo> GetPaymentInfo(string paymentLinkId);
    Task<Result<PayOsService.PaymentLinkResponse>> CreatePaymentUrlRegisterCreator(Guid orderId);
    Task<ApiResponse> HandlePaymentCallback(string paymentLinkId, long orderCode);
    Task<Result> HandlePayOsWebhook(WebhookType payload, string signatureFromPayOs, string requestBody);
    Task<ApiResponse> ConfirmWebhook(string webhookUrl);
}