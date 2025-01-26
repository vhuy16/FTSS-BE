namespace FTSS_API.Payload.Pay;

public class ExtendedPaymentInfo
{
    
    public int Amount { get; set; }
    public string Description { get; set; }
    public List<CustomItemData> Items { get; set; }
    public string BuyerName { get; set; }
    public string BuyerPhone { get; set; }
    public string BuyerEmail { get; set; }
    public string BuyerAddress { get; set; }
    public long OrderCode {get; set;}

    public string Status { get; set; }
    public Guid ProductId { get; set; }
}