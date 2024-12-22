namespace FTSS_API.Service.Implement.Implement;

public interface IEmailSender
{
   Task SendVerificationEmailAsync(string email, string otp);
}