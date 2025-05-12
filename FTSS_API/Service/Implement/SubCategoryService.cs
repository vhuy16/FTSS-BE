using AutoMapper;
using FTSS_API.Payload.Request.SubCategory;
using FTSS_API.Payload;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Repository.Interface;
using FTSS_Model.Entities;
using FTSS_API.Payload.Response.SubCategory;
using FTSS_API.Utils;
using FTSS_Model.Paginate;
using Microsoft.EntityFrameworkCore;


namespace FTSS_API.Service.Implement
{
    public class SubCategoryService : BaseService<SubCategoryService>, ISubCategoryService
    {
        public SubCategoryService(IUnitOfWork<MyDbContext> unitOfWork,
            ILogger<SubCategoryService> logger,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
            ) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {

        }
        public async Task<ApiResponse> CreateSubCategory(SubCategoryRequest request)
        {
            // Kiểm tra trường SubCategoryName không được rỗng
            if (string.IsNullOrWhiteSpace(request.SubCategoryName))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "SubCategoryName không được để trống.",
                    data = null
                };
            }

            // Kiểm tra Description không được rỗng
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Description không được để trống.",
                    data = null
                };
            }

            // Kiểm tra tồn tại của Category
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: c => c.Id == request.CategoryId && c.IsDelete == false);

            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Category không tồn tại.",
                    data = null
                };
            }

            // Kiểm tra trùng SubCategoryName
            var existingSubCategory = await _unitOfWork.GetRepository<SubCategory>().SingleOrDefaultAsync(
                predicate: s => s.SubCategoryName.Equals(request.SubCategoryName) && s.IsDelete == false);

            if (existingSubCategory != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "SubCategoryName đã tồn tại.",
                    data = null
                };
            }

            // Tạo mới SubCategory
            var subCategory = new SubCategory
            {
                Id = Guid.NewGuid(),
                SubCategoryName = request.SubCategoryName,
                Description = request.Description,
                CategoryId = request.CategoryId,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                ModifyDate = TimeUtils.GetCurrentSEATime()
            };

            await _unitOfWork.GetRepository<SubCategory>().InsertAsync(subCategory);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể tạo SubCategory.",
                    data = null
                };
            }

            var response = new SubCategoryResponse
            {
                Id = subCategory.Id,
                SubCategoryName = subCategory.SubCategoryName,
                Description = subCategory.Description,
                CategoryId = subCategory.CategoryId,
                CreateDate = subCategory.CreateDate,
                ModifyDate = subCategory.ModifyDate,
                CategoryName = category.CategoryName 
            };

            return new ApiResponse
            {
                status = StatusCodes.Status201Created.ToString(),
                message = "SubCategory đã được tạo thành công.",
                data = response
            };
        }


        public async Task<ApiResponse> DeleteSubCategory(Guid id)
        {
            // Tìm SubCategory chưa bị xóa
            var subCategory = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == false);

            if (subCategory == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "SubCategory không tồn tại hoặc đã bị xóa.",
                    data = null
                };
            }

            // Kiểm tra SubCategory có chứa Product còn sống không
            var productExists = await _unitOfWork.GetRepository<Product>()
                .GetQueryable()
                .Where(p => p.SubCategoryId == id && p.IsDelete == false)
                .AnyAsync();

            if (productExists)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Không thể xóa SubCategory vì đang chứa sản phẩm còn hoạt động.",
                    data = null
                };
            }

            // Đánh dấu là đã xóa
            subCategory.IsDelete = true;
            _unitOfWork.GetRepository<SubCategory>().UpdateAsync(subCategory);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể xóa SubCategory.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "SubCategory đã được xóa thành công.",
                data = null
            };
        }


        public async Task<ApiResponse> EnableSubCategory(Guid id)
        {
            // Kiểm tra sự tồn tại của SubCategory đã bị xóa
            var subCategory = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(predicate: sc => sc.Id == id && sc.IsDelete == true);

            // Nếu không tìm thấy SubCategory
            if (subCategory == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "SubCategory không tồn tại hoặc chưa bị xóa.",
                    data = null
                };
            }

            // Cập nhật IsDelete thành false
            subCategory.IsDelete = false;
            _unitOfWork.GetRepository<SubCategory>().UpdateAsync(subCategory);

            // Lưu thay đổi
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            // Trả về kết quả
            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể kích hoạt lại SubCategory.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "SubCategory đã được kích hoạt lại thành công.",
                data = null
            };
        }


        public async Task<ApiResponse> GetAllSubCategories(int page, int size, string searchName, bool? isAscending)
        {
            var subCategories = await _unitOfWork.GetRepository<SubCategory>().GetPagingListAsync(
                selector: s => new SubCategoryResponse
                {
                    Id = s.Id,
                    SubCategoryName = s.SubCategoryName,
                    Description = s.Description,
                    CategoryId = s.CategoryId,
                    CreateDate = s.CreateDate,
                    ModifyDate = s.ModifyDate,
                    CategoryName = s.Category.CategoryName,
                    IsDelete = s.IsDelete,
                },
                predicate: s => 
                                (string.IsNullOrEmpty(searchName) || s.SubCategoryName.Contains(searchName)),
                orderBy: q => isAscending.HasValue
                    ? (isAscending.Value ? q.OrderBy(s => s.SubCategoryName) : q.OrderByDescending(s => s.SubCategoryName))
                    : q.OrderByDescending(s => s.CreateDate),
                size: size,
                page: page);

            // Tính toán số lượng tổng cộng và số trang
            int totalItems = subCategories.Total;
            int totalPages = (int)Math.Ceiling((double)totalItems / size);

            // Kiểm tra nếu không có dữ liệu
            if (subCategories == null || subCategories.Items.Count == 0)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "No subcategories found.",
                    data = new Paginate<SubCategoryResponse>
                    {
                        Page = page,
                        Size = size,
                        Total = 0,
                        TotalPages = 0,
                        Items = new List<SubCategoryResponse>()
                    }
                };
            }

            // Trả về danh sách SubCategories
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "SubCategories retrieved successfully.",
                data = new Paginate<SubCategoryResponse>
                {
                    Page = page,
                    Size = size,
                    Total = totalItems,
                    TotalPages = totalPages,
                    Items = subCategories.Items
                }
            };
        }


        public async Task<ApiResponse> GetSubCategory(Guid id)
        {
            // Kiểm tra ID không được rỗng
            if (id == Guid.Empty)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "ID không hợp lệ.",
                    data = null
                };
            }

            // Lấy thông tin SubCategory theo ID
            var subCategory = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(
                    selector: s => new  
                    {
                        Id = s.Id,
                        SubCategoryName = s.SubCategoryName,
                        Description = s.Description,
                        CategoryId = s.CategoryId,
                        CategoryName = s.Category.CategoryName, // Bao gồm CategoryName
                        CreateDate = s.CreateDate,
                        ModifyDate = s.ModifyDate
                    },
                    predicate: s => s.Id == id && s.IsDelete == false
                );

            // Kiểm tra nếu SubCategory không tồn tại
            if (subCategory == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "SubCategory không tồn tại.",
                    data = null
                };
            }

            // Trả về kết quả
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "SubCategory được lấy thành công.",
                data = new SubCategoryResponse
                {
                    Id = subCategory.Id,
                    SubCategoryName = subCategory.SubCategoryName,
                    Description = subCategory.Description,
                    CategoryId = subCategory.CategoryId,
                    CategoryName = subCategory.CategoryName,
                    CreateDate = subCategory.CreateDate,
                    ModifyDate = subCategory.ModifyDate
                }
            };
        }


        public async Task<ApiResponse> UpdateSubCategory(Guid id, SubCategoryRequest request)
        {
            // Kiểm tra trường SubCategoryName không được rỗng
            if (string.IsNullOrWhiteSpace(request.SubCategoryName))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "SubCategoryName không được để trống.",
                    data = null
                };
            }

            // Kiểm tra Description không được rỗng
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Description không được để trống.",
                    data = null
                };
            }

            // Kiểm tra sự tồn tại của SubCategory
            var existingSubCategory = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(predicate: s => s.Id == id && s.IsDelete == false);

            if (existingSubCategory == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "SubCategory không tồn tại.",
                    data = null
                };
            }

            // Kiểm tra trùng SubCategoryName
            var duplicateSubCategory = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(predicate: s => s.SubCategoryName == request.SubCategoryName && s.Id != id && s.IsDelete == false);

            if (duplicateSubCategory != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "SubCategoryName đã tồn tại.",
                    data = null
                };
            }

            // Cập nhật thông tin SubCategory
            existingSubCategory.SubCategoryName = request.SubCategoryName;
            existingSubCategory.Description = request.Description;
            existingSubCategory.CategoryId = request.CategoryId;
            existingSubCategory.ModifyDate = TimeUtils.GetCurrentSEATime();

            _unitOfWork.GetRepository<SubCategory>().UpdateAsync(existingSubCategory);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể cập nhật SubCategory.",
                    data = null
                };
            }

            // Lấy CategoryName từ bảng Category
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: c => c.Id == existingSubCategory.CategoryId && c.IsDelete == false);

            string categoryName = category?.CategoryName ?? "Unknown";

            // Trả về response
            var response = new SubCategoryResponse
            {
                Id = existingSubCategory.Id,
                SubCategoryName = existingSubCategory.SubCategoryName,
                Description = existingSubCategory.Description,
                CategoryId = existingSubCategory.CategoryId,
                CategoryName = categoryName, 
                CreateDate = existingSubCategory.CreateDate,
                ModifyDate = existingSubCategory.ModifyDate,
            };

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "SubCategory đã được cập nhật thành công.",
                data = response
            };
        }
    }
}
