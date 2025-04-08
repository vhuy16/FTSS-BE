namespace FTSS_API.Utils;

public class EmailTemplatesUtils
{
   
        public static string VerificationEmailTemplate(string otp) => $@"
         <div style='font-family: Arial, sans-serif; color: #333;'>
             <h2>Your OTP Code</h2>
             <p>Dear User,</p>
             <p>Thank you for using our service! To complete your verification process, please use the following OTP (One-Time Password) code:</p>
             <h1 style='color: #2E86C1;'>{otp}</h1>
             <p>This code is valid for the next 10 minutes. Please do not share this code with anyone.</p>
             <h3>What happens next?</h3>
             <p>Once you've entered the OTP code, you'll be able to complete the verification process and access your account.</p>
             <p>If you did not request this code, please disregard this message. If you keep receiving OTPs without making any requests, we recommend updating your account security.</p>
             <p>Thank you for choosing our service!</p>
             <p>Best regards,<br>Your Company Name Support Team</p>
             <p style='font-size: 12px; color: #888;'>If you have any questions, feel free to <a href='mailto:support@yourcompany.com'>contact our support team</a>.</p>
         </div>";

        public static string RefundNotificationEmailTemplate(string orderId, bool isPaid) => $@"
         <div style='font-family: Arial, sans-serif; color: #333;'>
             <h2>Order Cancellation Notification</h2>
             <p>Dear User,</p>
             <p>We would like to inform you that your order (ID: <strong>{orderId}</strong>) has been cancelled.</p>
             {(isPaid ? 
                 "<h3 style='color: #2E86C1;'>Action Required</h3>" +
                 "<p>Since your order was already paid, please visit our website to update your bank account details so we can process your refund.</p>" +
                 "<p><a href='https://yourwebsite.com/account/settings' style='color: #2E86C1; text-decoration: none;'>Click here to update your bank details</a></p>" +
                 "<h3>What happens next?</h3>" +
                 "<p>Once we receive your updated bank account information, we will process your refund within 5-7 business days. You’ll receive a confirmation email once the refund is completed.</p>"
                 :
                 "<p>Since this order has not been paid, no further action is required from your side. This is just a notification to keep you informed.</p>")}
             <p>If you did not cancel this order or believe this is an error, please contact us immediately.</p>
             <p>We apologize for any inconvenience caused and thank you for your understanding.</p>
             <p>Best regards,<br>Your Company Name Support Team</p>
             <p style='font-size: 12px; color: #888;'>If you have any questions, feel free to <a href='mailto:support@yourcompany.com'>contact our support team</a>.</p>
         </div>";
}