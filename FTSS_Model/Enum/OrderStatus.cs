public enum OrderStatus
{
    PENDING_PAYMENT, // Chờ thanh toán
    PAID,            // Đã thanh toán
    PROCESSING,      // Đang xử lý
    SHIPPED,         // Đã giao hàng
    DELIVERED,       // Đã giao thành công
    CANCELLED,       // Đã hủy
    REFUNDED,        // Đã hoàn tiền
    FAILED,
    PENDING_DELIVERY,
    COMPLETED,
}