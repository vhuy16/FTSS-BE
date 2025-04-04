﻿using FTSS_API.Payload;

namespace FTSS_API.Service.Interface;

public interface IVnPayService
{
    Task<string> CreatePaymentUrl(Guid? orderId, Guid? bookingId);
    Task<ApiResponse> HandleCallBack(string status, Guid orderId);
}