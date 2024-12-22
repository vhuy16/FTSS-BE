using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Shipment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? ShippingAddress { get; set; }

    public decimal? ShippingFee { get; set; }

    public string? DeliveryStatus { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public string? DeliveryAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
