﻿namespace FTSS_API.Service.Implement.Implement;

public interface IEmailSender
{
   Task SendVerificationEmailAsync(string email, string otp);
   Task SendRefundNotificationEmailAsync(string email, string message);
   Task SendReturnAcceptedEmailAsync(string email, string message);
    Task RefundBookingNotificationEmailAsync(string email, string message);
}