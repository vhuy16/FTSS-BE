namespace FTSS_API.Payload.Request.Pay;

public class CreatePaymentRequest
{
    public Guid? OrderId { get; set; }  // Nếu thanh toán cho Order
    public Guid? BookingId { get; set; } // Nếu thanh toán cho Booking
    public string? PaymentMethod { get; set; }  // Ví dụ: "CreditCard", "BankTransfer"
    public decimal AmountPaid { get; set; } // Tổng số tiền thanh toán
}