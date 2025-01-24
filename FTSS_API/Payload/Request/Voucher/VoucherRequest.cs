namespace FTSS_API.Payload.Request.Voucher
{
    public class VoucherRequest
    {
        public decimal? Discount { get; set; }
        public int? Quantity { get; set; }
        public decimal? MaximumOrderValue { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? DiscountType { get; set; }

        public string? Description { get; set; }
    }
}
