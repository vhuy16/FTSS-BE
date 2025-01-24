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
                // Kiểm tra tính hợp lệ của dữ liệu đầu vào
                if (voucherRequest.Discount <= 0)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Discount must be greater than 0.",
                        data = null
                    };
                }

                if (voucherRequest.Quantity <= 0)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Quantity must be greater than 0.",
                        data = null
                    };
                }

                if (voucherRequest.ExpiryDate <= TimeUtils.GetCurrentSEATime())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Expiry date must be in the future.",
                        data = null
                    };
                }

                // Kiểm tra DiscountType có hợp lệ hay không
                if (!Enum.TryParse(typeof(VoucherTypeEnum), voucherRequest.DiscountType, true, out var discountTypeEnum))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid DiscountType. Allowed values are: Percentage, Fixed.",
                        data = null
                    };
                }

                // Nếu DiscountType là Fixed, tự động gán MaximumOrderValue bằng Discount nếu chưa hợp lệ
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
                        message = "Maximum order value must be greater than 0 for Percentage discount.",
                        data = null
                    };
                }

                // Tạo mã voucher ngẫu nhiên và kiểm tra trùng lặp
                string voucherCode;
                do
                {
                    voucherCode = GenerateVoucherCode();
                }
                while (await _unitOfWork.Context.Set<Voucher>().AnyAsync(v => v.VoucherCode == voucherCode));

                // Tạo đối tượng voucher mới
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
                    DiscountType = discountTypeEnum.ToString(), // Lưu kiểu Enum dưới dạng chuỗi
                    Description = voucherRequest.Description
                };

                // Lưu voucher vào database
                await _unitOfWork.Context.Set<Voucher>().AddAsync(newVoucher);
                await _unitOfWork.CommitAsync();

                // Tạo đối tượng VoucherResponse để trả về
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

                // Trả về kết quả thành công
                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Voucher added successfully.",
                    data = voucherResponse
                };
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra lỗi trong quá trình xử lý
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while adding the voucher.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> DeleteVoucher(Guid id)
        {
            try
            {
                // Lấy voucher từ cơ sở dữ liệu
                var voucher = await _unitOfWork.Context.Set<Voucher>().FirstOrDefaultAsync(v => v.Id == id && v.IsDelete == false);

                // Kiểm tra nếu voucher không tồn tại hoặc đã bị xóa
                if (voucher == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Voucher not found or already deleted.",
                        data = null
                    };
                }

                // Cập nhật trạng thái IsDelete thành true
                voucher.IsDelete = true;

                // Cập nhật ModifyDate
                voucher.ModifyDate = TimeUtils.GetCurrentSEATime();

                // Lưu thay đổi vào cơ sở dữ liệu
                _unitOfWork.Context.Set<Voucher>().Update(voucher);
                await _unitOfWork.CommitAsync();

                // Trả về kết quả thành công
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Voucher deleted successfully.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra lỗi trong quá trình xử lý
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while deleting the voucher.",
                    data = ex.Message
                };
            }
        }


        public async Task<ApiResponse> GetAllVoucher(int pageNumber, int pageSize, bool? isAscending, string? status, string? discountType)
        {
            try
            {
                // Lấy danh sách voucher không bị xóa
                var query = _unitOfWork.Context.Set<Voucher>().Where(v => v.IsDelete == false);

                // Áp dụng bộ lọc nếu có
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(v => v.Status == status);
                }

                if (!string.IsNullOrEmpty(discountType))
                {
                    query = query.Where(v => v.DiscountType == discountType);
                }

                // Sắp xếp (mặc định theo ngày tạo)
                if (isAscending.HasValue && isAscending.Value)
                {
                    query = query.OrderBy(v => v.CreateDate);
                }
                else
                {
                    query = query.OrderByDescending(v => v.CreateDate);
                }

                // Phân trang
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

                // Trả về kết quả
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Vouchers retrieved successfully.",
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
                // Xử lý lỗi
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving vouchers.",
                    data = ex.Message
                };
            }
        }


        public async Task<ApiResponse> GetListVoucher(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                // Xác định thời gian hiện tại
                var currentTime = TimeUtils.GetCurrentSEATime();

                // Lấy danh sách voucher từ database với các điều kiện lọc
                var query = _unitOfWork.Context.Set<Voucher>()
                    .Where(v => !v.IsDelete.HasValue || v.IsDelete == false) // isDelete = false
                    .Where(v => v.Status == VoucherEnum.Active.GetDescriptionFromEnum()) // status = Active
                    .Where(v => v.ExpiryDate > currentTime) // expiryDate chưa hết hạn
                    .Where(v => v.Quantity > 0);

                // Sắp xếp theo isAscending hoặc mặc định giảm dần theo ngày hết hạn
                query = isAscending.HasValue && isAscending.Value
                    ? query.OrderBy(v => v.ExpiryDate)
                    : query.OrderByDescending(v => v.ExpiryDate);

                // Phân trang
                var totalRecords = await query.CountAsync();
                var vouchers = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Chuyển đổi danh sách voucher sang danh sách response
                var responseList = vouchers.Select(v => new GetListVoucherResponse
                {
                    Id = v.Id,
                    VoucherCode = v.VoucherCode,
                    Description = v.Description,
                    ExpiryDate = v.ExpiryDate
                }).ToList();

                // Trả về kết quả
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of vouchers retrieved successfully.",
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
                // Trả về lỗi nếu có exception
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the list of vouchers.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> UpdateVoucher(Guid id, VoucherRequest voucherRequest)
        {
            try
            {
                // Lấy voucher từ database
                var voucher = await _unitOfWork.Context.Set<Voucher>().FirstOrDefaultAsync(v => v.Id == id && v.IsDelete == false);

                if (voucher == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Voucher not found or has been deleted.",
                        data = null
                    };
                }

                // Kiểm tra và cập nhật Discount nếu có sự thay đổi
                if (voucherRequest.Discount.HasValue && voucherRequest.Discount != voucher.Discount)
                {
                    if (voucherRequest.Discount <= 0)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Discount must be greater than 0.",
                            data = null
                        };
                    }

                    voucher.Discount = voucherRequest.Discount.Value;
                }

                // Kiểm tra và cập nhật Quantity nếu có sự thay đổi
                if (voucherRequest.Quantity.HasValue && voucherRequest.Quantity != voucher.Quantity)
                {
                    if (voucherRequest.Quantity <= 0)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Quantity must be greater than 0.",
                            data = null
                        };
                    }

                    voucher.Quantity = voucherRequest.Quantity.Value;
                }

                // Kiểm tra và cập nhật MaximumOrderValue nếu có sự thay đổi
                if (voucherRequest.MaximumOrderValue.HasValue && voucherRequest.MaximumOrderValue != voucher.MaximumOrderValue)
                {
                    if (voucherRequest.MaximumOrderValue <= 0)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Maximum order value must be greater than 0.",
                            data = null
                        };
                    }

                    voucher.MaximumOrderValue = voucherRequest.MaximumOrderValue.Value;
                }

                // Kiểm tra và cập nhật ExpiryDate nếu có sự thay đổi
                if (voucherRequest.ExpiryDate.HasValue && voucherRequest.ExpiryDate != voucher.ExpiryDate)
                {
                    if (voucherRequest.ExpiryDate <= TimeUtils.GetCurrentSEATime())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Expiry date must be in the future.",
                            data = null
                        };
                    }

                    voucher.ExpiryDate = voucherRequest.ExpiryDate.Value;
                }

                // Kiểm tra và cập nhật Description nếu có sự thay đổi
                if (!string.IsNullOrEmpty(voucherRequest.Description) && voucherRequest.Description != voucher.Description)
                {
                    voucher.Description = voucherRequest.Description;
                }

                // Kiểm tra và cập nhật DiscountType nếu có sự thay đổi
                if (!string.IsNullOrEmpty(voucherRequest.DiscountType) && voucherRequest.DiscountType != voucher.DiscountType)
                {
                    // Kiểm tra DiscountType có hợp lệ không
                    if (!Enum.TryParse(voucherRequest.DiscountType, out VoucherTypeEnum discountTypeEnum))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Invalid DiscountType. Must be 'Percentage' or 'Fixed'.",
                            data = null
                        };
                    }

                    voucher.DiscountType = voucherRequest.DiscountType;
                }

                // Cập nhật ModifyDate
                voucher.ModifyDate = TimeUtils.GetCurrentSEATime();

                // Lưu thay đổi vào database
                _unitOfWork.Context.Set<Voucher>().Update(voucher);
                await _unitOfWork.CommitAsync();

                // Tạo đối tượng VoucherResponse
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

                // Trả về kết quả thành công
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Voucher updated successfully.",
                    data = voucherResponse
                };
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra lỗi trong quá trình xử lý
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while updating the voucher.",
                    data = ex.Message
                };
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
