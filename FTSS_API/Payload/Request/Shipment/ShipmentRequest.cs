namespace FTSS_API.Payload.Request.Shipment;

public class ShipmentRequest
{
    public Guid OrderId { get; set; }
    public string? ShippingAddress { get; set; }
    public decimal? ShippingFee { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? DeliveryAt { get; set; }
}