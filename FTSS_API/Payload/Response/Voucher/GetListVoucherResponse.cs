namespace FTSS_API.Payload.Response.Voucher
{
    public class GetListVoucherResponse
    {
        public string VoucherCode { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
