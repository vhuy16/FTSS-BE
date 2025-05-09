using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Voucher;
using FTSS_API.Payload.Response.Voucher;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FTSS_API.Service.Implement
{
    public class VoucherService : BaseService<SubCategoryService>, IVoucherService
    {
        public VoucherService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<SubCategoryService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<ApiResponse> AddVoucher(VoucherRequest voucherRequest)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }

                if (voucherRequest.Discount <= 0)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Giảm giá phải lớn hơn 0.",
                        data = null
                    };
                }

                if (voucherRequest.Quantity <= 0)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Số lượng phải lớn hơn 0.",
                        data = null
                    };
                }

                if (voucherRequest.ExpiryDate <= TimeUtils.GetCurrentSEATime())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Ngày hết hạn phải trong tương lai.",
                        data = null
                    };
                }

                if (!Enum.TryParse(typeof(VoucherTypeEnum), voucherRequest.DiscountType, true, out var discountTypeEnum))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Loại giảm giá không hợp lệ. Giá trị cho phép: Percentage, Fixed.",
                        data = null
                    };
                }

                if ((VoucherTypeEnum)discountTypeEnum == VoucherTypeEnum.Fixed)
                {
                    if (!voucherRequest.MaximumOrderValue.HasValue || voucherRequest.MaximumOrderValue < voucherRequest.Discount)
                    {
                        voucherRequest.MaximumOrderValue = voucherRequest.Discount;
                    }
                }
                else if (voucherRequest.MaximumOrderValue <= 0)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Giá trị đơn hàng tối đa phải lớn hơn 0 đối với giảm giá phần trăm.",
                        data = null
                    };
                }

                string voucherCode;
                do
                {
                    voucherCode = GenerateVoucherCode();
                }
                while (await _unitOfWork.Context.Set<Voucher>().AnyAsync(v => v.VoucherCode == voucherCode));

                var newVoucher = new Voucher
                {
                    Id = Guid.NewGuid(),
                    VoucherCode = voucherCode,
                    Discount = voucherRequest.Discount ?? 0,
                    CreateDate = TimeUtils.GetCurrentSEATime(),
                    ModifyDate = TimeUtils.GetCurrentSEATime(),
                    IsDelete = false,
                    Status = VoucherEnum.Active.GetDescriptionFromEnum(),
                    Quantity = voucherRequest.Quantity,
                    MaximumOrderValue = voucherRequest.MaximumOrderValue,
                    ExpiryDate = voucherRequest.ExpiryDate,
                    DiscountType = discountTypeEnum.ToString(),
                    Description = voucherRequest.Description
                };

                await _unitOfWork.Context.Set<Voucher>().AddAsync(newVoucher);
                await _unitOfWork.CommitAsync();

                var voucherResponse = new VoucherResponse
                {
                    Id = newVoucher.Id,
                    VoucherCode = newVoucher.VoucherCode,
                    Discount = newVoucher.Discount,
                    CreateDate = newVoucher.CreateDate,
                    ModifyDate = newVoucher.ModifyDate,
                    IsDelete = newVoucher.IsDelete,
                    Status = newVoucher.Status,
                    Quantity = newVoucher.Quantity,
                    MaximumOrderValue = newVoucher.MaximumOrderValue,
                    ExpiryDate = newVoucher.ExpiryDate,
                    DiscountType = newVoucher.DiscountType,
                    Description = newVoucher.Description
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Thêm voucher thành công.",
                    data = voucherResponse
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi thêm voucher.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetAllVoucher(int pageNumber, int pageSize, bool? isAscending, string? status, string? discountType)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }
                var query = _unitOfWork.Context.Set<Voucher>().Where(v => v.IsDelete == false);
                await UpdateExpiredVouchersAsync();
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(v => v.Status == status);
                }

                if (!string.IsNullOrEmpty(discountType))
                {
                    query = query.Where(v => v.DiscountType == discountType);
                }

                if (isAscending.HasValue && isAscending.Value)
                {
                    query = query.OrderBy(v => v.CreateDate);
                }
                else
                {
                    query = query.OrderByDescending(v => v.CreateDate);
                }

                var totalRecords = await query.CountAsync();
                var vouchers = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(v => new
                    {
                        v.Id,
                        v.VoucherCode,
                        v.Discount,
                        v.Description,
                        v.Status,
                        v.DiscountType,
                        v.Quantity,
                        v.MaximumOrderValue,
                        v.ExpiryDate,
                        v.CreateDate,
                        v.ModifyDate
                    })
                    .ToListAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách voucher thành công.",
                    data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Vouchers = vouchers
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách voucher.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListVoucher(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                var currentTime = TimeUtils.GetCurrentSEATime();

                var query = _unitOfWork.Context.Set<Voucher>()
                    .Where(v => !v.IsDelete.HasValue || v.IsDelete == false)
                    .Where(v => v.Status == VoucherEnum.Active.GetDescriptionFromEnum())
                    .Where(v => v.ExpiryDate > currentTime)
                    .Where(v => v.Quantity > 0);

                query = isAscending.HasValue && isAscending.Value
                    ? query.OrderBy(v => v.ExpiryDate)
                    : query.OrderByDescending(v => v.ExpiryDate);

                var totalRecords = await query.CountAsync();
                var vouchers = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var responseList = vouchers.Select(v => new GetListVoucherResponse
                {
                    Id = v.Id,
                    VoucherCode = v.VoucherCode,
                    Description = v.Description,
                    ExpiryDate = v.ExpiryDate,
                    CreateDate = v.CreateDate,
                    Discount = v.Discount,
                    DiscountType = v.DiscountType,
                    MaximumOrderValue = v.MaximumOrderValue,
                    ModifyDate = v.ModifyDate,
                    Quantity = v.Quantity,
                    Status = v.Status
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách voucher khả dụng thành công.",
                    data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Vouchers = responseList
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách voucher.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> UpdateStatusVoucher(Guid id, string? status)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }
                if (string.IsNullOrEmpty(status) || !Enum.TryParse<VoucherEnum>(status, out var validStatus))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Trạng thái không hợp lệ.",
                        data = null
                    };
                }

                var voucher = await _unitOfWork.Context.Set<Voucher>().FirstOrDefaultAsync(v => v.Id == id && v.IsDelete == false);
                if (voucher == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy voucher hoặc đã bị xóa.",
                        data = null
                    };
                }

                voucher.Status = validStatus.ToString();
                voucher.ModifyDate = TimeUtils.GetCurrentSEATime();

                _unitOfWork.GetRepository<Voucher>().UpdateAsync(voucher);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cập nhật trạng thái voucher thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi cập nhật trạng thái voucher.",
                    data = null
                };
            }
        }

        public async Task<ApiResponse> UpdateVoucher(Guid id, VoucherRequest voucherRequest)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }
                var voucher = await _unitOfWork.Context.Set<Voucher>().FirstOrDefaultAsync(v => v.Id == id && v.IsDelete == false);

                if (voucher == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy voucher hoặc đã bị xóa.",
                        data = null
                    };
                }

                if (voucherRequest.Discount.HasValue && voucherRequest.Discount != voucher.Discount)
                {
                    if (voucherRequest.Discount <= 0)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Giảm giá phải lớn hơn 0.",
                            data = null
                        };
                    }

                    voucher.Discount = voucherRequest.Discount.Value;
                }

                if (voucherRequest.Quantity.HasValue && voucherRequest.Quantity != voucher.Quantity)
                {
                    if (voucherRequest.Quantity <= 0)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Số lượng phải lớn hơn 0.",
                            data = null
                        };
                    }

                    voucher.Quantity = voucherRequest.Quantity.Value;
                }

                if (voucherRequest.MaximumOrderValue.HasValue && voucherRequest.MaximumOrderValue != voucher.MaximumOrderValue)
                {
                    if (voucherRequest.MaximumOrderValue <= 0)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Giá trị đơn hàng tối đa phải lớn hơn 0.",
                            data = null
                        };
                    }

                    voucher.MaximumOrderValue = voucherRequest.MaximumOrderValue.Value;
                }

                if (voucherRequest.ExpiryDate.HasValue && voucherRequest.ExpiryDate != voucher.ExpiryDate)
                {
                    if (voucherRequest.ExpiryDate <= TimeUtils.GetCurrentSEATime())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Ngày hết hạn phải trong tương lai.",
                            data = null
                        };
                    }

                    voucher.ExpiryDate = voucherRequest.ExpiryDate.Value;
                }

                if (!string.IsNullOrEmpty(voucherRequest.Description) && voucherRequest.Description != voucher.Description)
                {
                    voucher.Description = voucherRequest.Description;
                }

                if (!string.IsNullOrEmpty(voucherRequest.DiscountType) && voucherRequest.DiscountType != voucher.DiscountType)
                {
                    if (!Enum.TryParse(voucherRequest.DiscountType, out VoucherTypeEnum discountTypeEnum))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Loại giảm giá không hợp lệ. Phải là 'Percentage' hoặc 'Fixed'.",
                            data = null
                        };
                    }

                    voucher.DiscountType = voucherRequest.DiscountType;
                }

                voucher.ModifyDate = TimeUtils.GetCurrentSEATime();

                _unitOfWork.GetRepository<Voucher>().UpdateAsync(voucher);
                await _unitOfWork.CommitAsync();

                var voucherResponse = new VoucherResponse
                {
                    Id = voucher.Id,
                    VoucherCode = voucher.VoucherCode,
                    Discount = voucher.Discount,
                    CreateDate = voucher.CreateDate,
                    ModifyDate = voucher.ModifyDate,
                    IsDelete = voucher.IsDelete,
                    Status = voucher.Status,
                    Quantity = voucher.Quantity,
                    MaximumOrderValue = voucher.MaximumOrderValue,
                    ExpiryDate = voucher.ExpiryDate,
                    DiscountType = voucher.DiscountType,
                    Description = voucher.Description
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cập nhật voucher thành công.",
                    data = voucherResponse
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi cập nhật voucher.",
                    data = ex.Message
                };
            }
        }
        public async Task UpdateExpiredVouchersAsync()
        {
            var now = DateTime.UtcNow;

            // Lấy danh sách voucher hết hạn và chưa bị Inactive
            var expiredVouchers = await _unitOfWork.GetRepository<Voucher>().GetListAsync(
                predicate: v => v.ExpiryDate <= now && v.Status != VoucherEnum.Inactive.GetDescriptionFromEnum()
            );

            if (expiredVouchers.Any())
            {
                foreach (var voucher in expiredVouchers)
                {
                    voucher.Status = VoucherEnum.Inactive.GetDescriptionFromEnum();
                    voucher.ModifyDate = now;
                }

                _unitOfWork.GetRepository<Voucher>().UpdateRange(expiredVouchers);

                await _unitOfWork.CommitAsync();
            }
        }

        // Hàm tạo mã voucher ngẫu nhiên
        private string GenerateVoucherCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10) // 10 ký tự
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
