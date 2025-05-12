using System.Net;
using System.Net.Mail;
using FTSS_API.Payload.Request.Email;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Utils;
using Microsoft.Extensions.Options;

public class EmailSender : IEmailSender
{
    private readonly string _emailAddress;
    private readonly string _appPassword;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
    {
        _logger = logger;
        if (emailSettings == null)
        {
            _logger.LogError("IOptions<EmailSettings> is null in EmailSender constructor.");
            throw new ArgumentNullException(nameof(emailSettings), "Email settings are null.");
        }
        if (emailSettings.Value == null)
        {
            _logger.LogError("emailSettings.Value is null in EmailSender constructor.");
            throw new ArgumentNullException(nameof(emailSettings), "Email settings value is null.");
        }

        if (string.IsNullOrEmpty(emailSettings.Value.EmailAddress))
        {
            _logger.LogError("EmailAddress is null or empty in EmailSettings.");
            throw new ArgumentException("EmailAddress is null or empty in EmailSettings.");
        }

        if (string.IsNullOrEmpty(emailSettings.Value.AppPassword))
        {
            _logger.LogError("AppPassword is null or empty in EmailSettings.");
            throw new ArgumentException("AppPassword is null or empty in EmailSettings.");
        }

        _emailAddress = emailSettings.Value.EmailAddress;
        _appPassword = emailSettings.Value.AppPassword;

        _logger.LogInformation($"Email address : {_emailAddress}");
        _logger.LogInformation($"App password : {_appPassword}");
    }
    public Task SendReturnAcceptedEmailAsync(string email, string message)
    {
        return SendEmailAsync(email, "Return Request Accepted", message);
    }
    public Task SendVerificationEmailAsync(string email, string otp)
    {
        string message = EmailTemplatesUtils.VerificationEmailTemplate(otp);
        return SendEmailAsync(email, "OTP Verification", message);
    }

    // New method for sending refund notification
    public Task SendRefundNotificationEmailAsync(string email, string message)
    {
        return SendEmailAsync(email, "Refund Notification", message);
    }

    // Private helper method to send emails
    private Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var mailMessage = new MailMessage(_emailAddress, toEmail, subject, message)
        {
            IsBodyHtml = true
        };

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_emailAddress, _appPassword)
        };

        return client.SendMailAsync(mailMessage);
    }

    public Task RefundBookingNotificationEmailAsync(string email, string message)
    {
        return SendEmailAsync(email, "Refund Booking Notification", message);
    }
}