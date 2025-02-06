namespace FTSS_API.Payload.Pay;

public class WebhookNotification
{
    public string Code { get; set; } // Mã trạng thái chung của phản hồi từ webhook
    public string Desc { get; set; } // Mô tả trạng thái tổng quát
    public bool Success { get; set; } // Cờ thành công của giao dịch
    public WebhookNotificationData Data { get; set; } // Thông tin chi tiết về giao dịch
    public string Signature { get; set; } // Chữ ký để xác thực dữ liệu
}

public class WebhookNotificationData
{
    public int OrderCode { get; set; } // Mã đơn hàng
    public decimal Amount { get; set; } // Số tiền giao dịch
    public string Description { get; set; } // Mô tả giao dịch
    public string AccountNumber { get; set; } // Số tài khoản thực hiện giao dịch
    public string Reference { get; set; } // Mã tham chiếu giao dịch
    public string TransactionDateTime { get; set; } // Thời gian giao dịch
    public string Currency { get; set; } // Loại tiền tệ
    public string Code { get; set; } // Mã trạng thái của giao dịch trong `data`
    public string Desc { get; set; } // Mô tả trạng thái chi tiết của giao dịch
        
}
