namespace FTSS_API.Service.Interface;

public interface IVnPayService
{
    Task<string> CreatePaymentUrl(Guid orderId);
}