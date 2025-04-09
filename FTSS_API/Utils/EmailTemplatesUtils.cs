namespace FTSS_API.Utils;

public class EmailTemplatesUtils
{
    public static string VerificationEmailTemplate(string otp) => $@"
         <div style='font-family: Arial, sans-serif; color: #333;'>
             <h2>Mã OTP của bạn</h2>
             <p>Kính gửi Quý khách,</p>
             <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi! Để hoàn tất quá trình xác minh, vui lòng sử dụng mã OTP (Mật khẩu dùng một lần) sau đây:</p>
             <h1 style='color: #2E86C1;'>{otp}</h1>
             <p>Mã này có hiệu lực trong 10 phút tới. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
             <h3>Tiếp theo là gì?</h3>
             <p>Sau khi nhập mã OTP, bạn sẽ hoàn tất quá trình xác minh và có thể truy cập tài khoản của mình.</p>
             <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này. Nếu bạn liên tục nhận được OTP mà không thực hiện yêu cầu, chúng tôi khuyên bạn nên cập nhật bảo mật tài khoản.</p>
             <p>Cảm ơn bạn đã lựa chọn dịch vụ của chúng tôi!</p>
             <p>Trân trọng,<br>Đội ngũ Hỗ trợ Công ty Tên Công Ty</p>
             <p style='font-size: 12px; color: #888;'>Nếu bạn có bất kỳ câu hỏi nào, vui lòng <a href='mailto:support@yourcompany.com'>liên hệ với đội ngũ hỗ trợ của chúng tôi</a>.</p>
         </div>";

    public static string RefundNotificationEmailTemplate(Guid orderId, string orderCode, bool isPaid) => $@"
    <div style='font-family: Arial, sans-serif; color: #333;'>
        <h2>Thông báo Hủy Đơn hàng</h2>
        <p>Kính gửi Quý khách,</p>
        <p>Chúng tôi xin thông báo rằng đơn hàng của bạn (ID: <strong>{orderCode}</strong>) đã bị hủy.</p>
        {(isPaid ?
            "<h3 style='color: #2E86C1;'>Hành động cần thực hiện</h3>" +
            "<p>Vì đơn hàng của bạn đã được thanh toán, vui lòng truy cập trang web của chúng tôi để cập nhật thông tin tài khoản ngân hàng nhằm xử lý hoàn tiền.</p>" +
            $"<p><a href='https://ftss-fe.vercel.app/order-detail/{orderId}' style='color: #2E86C1; text-decoration: none;'>Nhấn vào đây để cập nhật thông tin ngân hàng</a></p>" +
            "<h3>Tiếp theo là gì?</h3>" +
            "<p>Sau khi chúng tôi nhận được thông tin tài khoản ngân hàng cập nhật của bạn, chúng tôi sẽ xử lý hoàn tiền trong vòng 5-7 ngày làm việc. Bạn sẽ nhận được email xác nhận khi quá trình hoàn tiền hoàn tất.</p>"
            :
            "<p>Vì đơn hàng này chưa được thanh toán, bạn không cần thực hiện thêm hành động nào. Đây chỉ là thông báo để bạn nắm thông tin.</p>")}
        <p>Nếu bạn không hủy đơn hàng này hoặc cho rằng đây là lỗi, vui lòng liên hệ với chúng tôi ngay lập tức.</p>
        <p>Chúng tôi xin lỗi vì bất kỳ sự bất tiện nào gây ra và cảm ơn sự thông cảm của bạn.</p>
        
        <p style='font-size: 12px; color: #888;'>Nếu bạn có bất kỳ câu hỏi nào, vui lòng <a href='mailto:support@yourcompany.com'>liên hệ với đội ngũ hỗ trợ của chúng tôi</a>.</p>
    </div>";
}