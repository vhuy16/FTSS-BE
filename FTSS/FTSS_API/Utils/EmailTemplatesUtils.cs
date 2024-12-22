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

    
}