namespace FTSS_API.Payload.Request.Shipment;

public class GoshipWebhookData
{
    public string Gcode { get; set; }
    public string Code { get; set; }
    public string OrderId { get; set; }
    public double Weight { get; set; }
    public int Fee { get; set; }
    public int Cod { get; set; }
    public int Payer { get; set; }
    public int Status { get; set; }
    public string Message { get; set; }
    public string TrackingUrl { get; set; }
}