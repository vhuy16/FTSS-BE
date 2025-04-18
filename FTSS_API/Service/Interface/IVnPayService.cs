using FTSS_API.Payload;
using FTSS_Model.Entities;

namespace FTSS_API.Service.Interface;

public interface IVnPayService
{
    Task<string> CreatePaymentUrl(Guid? orderId, Guid? bookingId);
    Task<(ApiResponse, Order)> HandleCallBack(string status, Guid orderId);
     Task<ApiResponse> CancelPendingTransactions(TimeSpan timeout);
}