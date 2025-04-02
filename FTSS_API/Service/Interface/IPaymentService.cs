using FTSS_API.Payload;
using FTSS_API.Payload.Request.Pay;

namespace FTSS_API.Service.Interface;

public interface IPaymentService
{
    Task<ApiResponse> CreatePayment(CreatePaymentRequest request);
    Task<ApiResponse> GetPaymentById(Guid paymentId);
    Task<ApiResponse> GetPaymentByOrderId(Guid orderId);
    Task<ApiResponse> GetPayments(int page, int size);
    Task<ApiResponse> UpdatePaymentStatus(Guid paymentId, string newStatus);
    Task<ApiResponse> UpdateBankInfor(Guid paymentId, long? bankNumber, string bankName, string bankHolder);
}