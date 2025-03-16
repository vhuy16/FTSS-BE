using AutoMapper;
using FTSS_Model.Entities;
using FTSS_API.Payload.Response.Shipment;
using FTSS_Model.Paginate;

public class ShipmentProfile : Profile
{
    public ShipmentProfile()
    {
        CreateMap<Shipment, ShipmentResponse>();
        CreateMap<Paginate<Shipment>, List<ShipmentResponse>>()
            .ConvertUsing((src, dest, context) => src.Items.Select(shipment => context.Mapper.Map<ShipmentResponse>(shipment)).ToList());
    }
}