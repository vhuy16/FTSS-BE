using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Shipment;
using FTSS_API.Payload.Response.Shipment;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Repository.Interface;

namespace FTSS_API.Service.Implement;

public class ShipmentService : BaseService<ShipmentService>, IShipmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShipmentService> _logger;
    private readonly IMapper _mapper;


    public ShipmentService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<ShipmentService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse> CreateShipment(ShipmentRequest request)
    {
        
        if (request == null) throw new ArgumentNullException(nameof(request));
       var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(predicate: p => p.Id == request.OrderId);
       if (order == null || order.Status != OrderStatus.PENDING_DELIVERY.ToString())
       {
           return new ApiResponse()
           {
               status = StatusCodes.Status400BadRequest.ToString(),
               message = "Invalid order",
               data = null
           };
       }
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            ShippingAddress = request.ShippingAddress,
            ShippingFee = request.ShippingFee,
            DeliveryStatus = "Pending",
            TrackingNumber = request.TrackingNumber,
            DeliveryDate = request.DeliveryDate,
            DeliveryAt = request.DeliveryAt
        };

        await _unitOfWork.GetRepository<Shipment>().InsertAsync(shipment);
        bool isSuccess = await _unitOfWork.CommitAsync() > 0;

        if(isSuccess){
        return new ApiResponse()
        {
            status = StatusCodes.Status201Created.ToString(),
            message = "Shipment created",
            data = _mapper.Map<ShipmentResponse>(shipment)
        };}
        else
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Unable to create shipment",
                data = null
            };
        }
    }

    public async Task<ApiResponse> GetAllShipments(int page, int pageSize, string? search = null)
    {
        var shipments = await _unitOfWork.GetRepository<Shipment>().GetPagingListAsync(
            predicate: s => string.IsNullOrEmpty(search) || s.TrackingNumber.Contains(search),
            page: page,
            size: pageSize);

        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Shipment list",
            data = _mapper.Map<List<Shipment>>(shipments)
        };
    }

    public async Task<ApiResponse> GetShipmentById(Guid id)
    {
        var shipment = await _unitOfWork.GetRepository<Shipment>().SingleOrDefaultAsync(predicate: s => s.Id.Equals(id));
        if (shipment == null)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Shipment not found",
                data = null
            };
        }

        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Shipment",
            data = _mapper.Map<Shipment>(shipment)
        };
    }

    public async Task<bool> UpdateShipment(Guid id, ShipmentRequest request)
    {
        var shipment =  await _unitOfWork.GetRepository<Shipment>().SingleOrDefaultAsync(predicate: s => s.Id.Equals(id));
        if (shipment == null) return false;

        shipment.ShippingAddress = request.ShippingAddress ?? shipment.ShippingAddress;
        shipment.ShippingFee = request.ShippingFee ?? shipment.ShippingFee;
        shipment.DeliveryStatus = request.DeliveryStatus ?? shipment.DeliveryStatus;
        shipment.TrackingNumber = request.TrackingNumber ?? shipment.TrackingNumber;
        shipment.DeliveryDate = request.DeliveryDate ?? shipment.DeliveryDate;
        shipment.DeliveryAt = request.DeliveryAt ?? shipment.DeliveryAt;

         _unitOfWork.GetRepository<Shipment>().UpdateAsync(shipment);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> DeleteShipment(Guid id)
    {
        var shipment =  await _unitOfWork.GetRepository<Shipment>().SingleOrDefaultAsync(predicate: s => s.Id.Equals(id));
        if (shipment == null) return false;

         _unitOfWork.GetRepository<Shipment>().DeleteAsync(shipment);
        await _unitOfWork.CommitAsync();
        return true;
    }
}
