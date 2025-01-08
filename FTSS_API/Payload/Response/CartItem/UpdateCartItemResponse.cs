﻿namespace FTSS_API.Payload.Response.CartItem
{
    public class UpdateCartItemResponse
    {
        public Guid? CartItemId { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Price { get; set; }
    }
}
