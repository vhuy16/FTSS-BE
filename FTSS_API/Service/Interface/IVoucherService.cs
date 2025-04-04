﻿using FTSS_API.Payload;
using FTSS_API.Payload.Request.Voucher;

namespace FTSS_API.Service.Interface
{
    public interface IVoucherService
    {
        Task<ApiResponse> AddVoucher(VoucherRequest voucherRequest);
        Task<ApiResponse> GetAllVoucher(int pageNumber, int pageSize, bool? isAscending, string? status, string? discountType);
        Task<ApiResponse> GetListVoucher(int pageNumber, int pageSize, bool? isAscending);
        Task<ApiResponse> UpdateStatusVoucher(Guid id, string? status);
        Task<ApiResponse> UpdateVoucher(Guid id, VoucherRequest voucherRequest);
    }
}
