﻿namespace FTSS_API.Payload.Response.CartItem
{
    public class AddCartItemResponse
    {
        public Guid CartItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal? Price { get; set; }
    }
}
