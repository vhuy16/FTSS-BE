using Net.payOS.Types;

namespace FTSS_API.Payload.Pay;

public record CustomItemData : ItemData
{
    public Guid ProductId { get; set; }

    public CustomItemData(string name, int quantity, int price, Guid productId)
        : base(name, quantity, price)
    {
        ProductId = productId;
    }
}