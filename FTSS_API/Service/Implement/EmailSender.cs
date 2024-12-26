using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using FTSS_API.Payload.Request.Email;
using FTSS_API.Utils;
using Microsoft.Extensions.Logging;

namespace FTSS_API.Service.Implement.Implement
{
    public class EmailSender : IEmailSender
    {
        private readonly string _emailAddress;
        private readonly string _appPassword;
        private readonly string subject = "OTP Verification";
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


        public Task SendVerificationEmailAsync(string email, string otp)
        {
            string message = EmailTemplatesUtils.VerificationEmailTemplate(otp);

            var mailMessage = new MailMessage(_emailAddress, email, subject, message)
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
    }
}