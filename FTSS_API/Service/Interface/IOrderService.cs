using FTSS_API.Payload;
using FTSS_API.Payload.Request;

namespace FTSS_API.Service.Interface;

public interface IOrderService
{
    Task<ApiResponse> CreateOrder(CreateOrderRequest createOrderRequest);
    Task<ApiResponse> GetListOrder(int page, int size, bool? isAscending);
    Task<ApiResponse> GetAllOrder(int page, int size, string status, bool? isAscending, string userName);
    Task<ApiResponse> GetOrderById(Guid id);
    // Task<ApiResponse> UpdateOrder(Guid id, OrderStatus? orderStatus, ShipEnum? shipStatus);
    Task<ApiResponse> CancelOrder(Guid id);
}