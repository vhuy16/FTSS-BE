using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Request.Order;
using FTSS_API.Payload.Request.Return;

namespace FTSS_API.Service.Interface;

public interface IOrderService
{
    Task<ApiResponse> CreateOrder(CreateOrderRequest createOrderRequest);
    Task<ApiResponse> GetListOrder(int page, int size, bool? isAscending, string? orderCode);

    Task<ApiResponse> GetAllOrder(int page, int size, string status, string orderCode, bool? isAscending);

    Task<ApiResponse> GetOrderById(Guid id);
    // Task<ApiResponse> UpdateOrder(Guid id, OrderStatus? orderStatus, ShipEnum? shipStatus);
    Task<ApiResponse> CancelOrder(Guid id);
    Task<ApiResponse> UpdateOrder(Guid orderId, UpdateOrderRequest updateOrderRequest);
    Task<ApiResponse> CreateReturnRequest(CreateReturnRequest request, Supabase.Client client);
    Task<ApiResponse> GetReturnRequest(Guid? returnRequestId, Supabase.Client client, int page = 1, int pageSize = 10);
    Task<ApiResponse> UpdateTime(Guid id, UpdateTimeRequest request);
}