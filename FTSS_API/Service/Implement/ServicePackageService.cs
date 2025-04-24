using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.ServicePackage;
using FTSS_API.Payload.Response.Book;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FTSS_API.Service.Implement
{
    public class ServicePackageService : BaseService<ServicePackageService>, IServicePackageService
    {
        public ServicePackageService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<ServicePackageService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<ApiResponse> AddServicePackage(ServicePackageRequest request)
        {
            // Validate dữ liệu
            if (string.IsNullOrWhiteSpace(request.ServiceName))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Tên gói dịch vụ không được để trống.",
                    data = null
                };
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Mô tả gói dịch vụ không được để trống.",
                    data = null
                };
            }

            if (request.Price <= 0)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Giá gói dịch vụ phải lớn hơn 0.",
                    data = null
                };
            }

            // Tạo gói dịch vụ mới
            var newServicePackage = new ServicePackage
            {
                Id = Guid.NewGuid(),
                ServiceName = request.ServiceName,
                Description = request.Description,
                Price = request.Price,
                Status = ServicePackageStatus.Available.GetDescriptionFromEnum(),
                IsDelete = false,
            };

            await _unitOfWork.GetRepository<ServicePackage>().InsertAsync(newServicePackage);
            bool isSaved = await _unitOfWork.CommitAsync() > 0;

            if (!isSaved)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể thêm gói dịch vụ.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status201Created.ToString(),
                message = "Thêm gói dịch vụ thành công.",
                data = newServicePackage
            };
        }

        public async Task<ApiResponse> UpdateServicePackage(Guid id, ServicePackageRequest request)
        {
            var existing = await _unitOfWork.GetRepository<ServicePackage>()
                .SingleOrDefaultAsync(predicate: sp => sp.Id == id && sp.IsDelete == false,
                                      orderBy: null,
                                      include: null);

            if (existing == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Gói dịch vụ không tồn tại hoặc đã bị xóa.",
                    data = null
                };
            }

            bool isModified = false;

            // Chỉ cập nhật khi có dữ liệu mới
            if (!string.IsNullOrWhiteSpace(request.ServiceName) && request.ServiceName != existing.ServiceName)
            {
                existing.ServiceName = request.ServiceName;
                isModified = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description != existing.Description)
            {
                existing.Description = request.Description;
                isModified = true;
            }

            if (request.Price > 0 && request.Price != existing.Price)
            {
                existing.Price = request.Price;
                isModified = true;
            }

            if (!isModified)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Không có thông tin nào được cập nhật.",
                    data = null
                };
            }

            _unitOfWork.GetRepository<ServicePackage>().UpdateAsync(existing);
            bool isSaved = await _unitOfWork.CommitAsync() > 0;

            if (!isSaved)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Cập nhật gói dịch vụ thất bại.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Cập nhật gói dịch vụ thành công.",
                data = existing
            };
        }
        public async Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending)
        {
            var servicePackages = await _unitOfWork.GetRepository<ServicePackage>().GetListAsync(
                predicate: sp => sp.Status == ServicePackageStatus.Available.ToString() && sp.IsDelete == false,
                orderBy: isAscending == true ? sp => sp.OrderBy(x => x.ServiceName) : sp => sp.OrderByDescending(x => x.ServiceName)
            );

            var paginatedList = servicePackages
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = paginatedList.Select(sp => new GetServicePackageResponse
            {
                Id = sp.Id,
                ServiceName = sp.ServiceName,
                Description = sp.Description,
                Price = sp.Price,
                Status = sp.Status,
                IsDelete = sp.IsDelete,
            }).ToList();

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Lấy danh sách gói dịch vụ khả dụng thành công.",
                data = response
            };
        }

        public async Task<ApiResponse> DeleteServicePackage(Guid id)
        {
            var servicePackage = await _unitOfWork.GetRepository<ServicePackage>()
    .SingleOrDefaultAsync(predicate: sp => sp.Id == id && sp.IsDelete == false,
                          orderBy: null,
                          include: null);

            if (servicePackage == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Gói dịch vụ không tồn tại hoặc đã bị xóa.",
                    data = null
                };
            }

            servicePackage.IsDelete = true;
            _unitOfWork.GetRepository<ServicePackage>().Update(servicePackage);
            bool isSaved = await _unitOfWork.CommitAsync() > 0;

            if (!isSaved)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể xóa gói dịch vụ.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Gói dịch vụ đã được xóa thành công.",
                data = null
            };
        }

        public async Task<ApiResponse> EnableServicePackage(Guid id)
        {
            var servicePackage = await _unitOfWork.GetRepository<ServicePackage>()
    .SingleOrDefaultAsync(predicate: sp => sp.Id == id && sp.IsDelete == false,
                          orderBy: null,
                          include: null);

            if (servicePackage == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Gói dịch vụ không tồn tại hoặc đã được kích hoạt.",
                    data = null
                };
            }

            servicePackage.IsDelete = false;
            _unitOfWork.GetRepository<ServicePackage>().Update(servicePackage);
            bool isSaved = await _unitOfWork.CommitAsync() > 0;

            if (!isSaved)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể kích hoạt lại gói dịch vụ.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Kích hoạt gói dịch vụ thành công.",
                data = null
            };
        }
    }
}
