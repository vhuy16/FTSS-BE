using FTSS_API.Payload;
using FTSS_API.Payload.Request.Shipment;

namespace FTSS_API.Service.Interface;

public interface IShipmentService
{
    Task<ApiResponse> CreateShipment(ShipmentRequest request);
    Task<ApiResponse> GetAllShipments(int page, int pageSize, string? search = null);
    Task<ApiResponse> GetShipmentById(Guid id);
    Task<bool> UpdateShipment(Guid id, ShipmentRequest request);
    Task<bool> DeleteShipment(Guid id);
}