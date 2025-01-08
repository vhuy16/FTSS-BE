using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Controller;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Category;
using FTSS_API.Payload.Response.Category;
using FTSS_API.Payload.Response.SubCategory;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
using FTSS_Repository.Interface;

namespace FTSS_API.Service.Implement
{
    public class CategoryService : BaseService<CategoryService>, ICategoryService
    {
        public CategoryService(IUnitOfWork<MyDbContext> unitOfWork,
            ILogger<CategoryService> logger,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
            ) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<ApiResponse> CreateCategory(CategoryRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.CategoryName))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "CategoryName cannot be empty.",
                    data = null
                };
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Description cannot be empty.",
                    data = null
                };
            }

            // Đảm bảo rằng gọi đúng overload của SingleOrDefaultAsync
            var categoryExist = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: c => c.CategoryName.Equals(request.CategoryName) &&
                        c.IsDelete == false);



            if (categoryExist != null)
            {
                // Nếu category tồn tại và bị đánh dấu là đã xóa, cho phép tạo mới category
                if ((bool)categoryExist.IsDelete)
                {
                    categoryExist.IsDelete = false; // Đánh dấu category không bị xóa nữa
                    categoryExist.ModifyDate = TimeUtils.GetCurrentSEATime(); // Cập nhật ModifyDate

                    // Cập nhật category đã tồn tại trong DB
                    _unitOfWork.GetRepository<Category>().UpdateAsync(categoryExist);
                    int isSuccessful = await _unitOfWork.CommitAsync(); // Commit thay đổi

                    if (isSuccessful <= 0)  // Kiểm tra có thành công không
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = "Failed to update existing category.",
                            data = null
                        };
                    }

                    return new ApiResponse
                    {
                        status = StatusCodes.Status200OK.ToString(),
                        message = "Category reactivated successfully.",
                        data = null
                    };
                }
                else
                {
                    // Nếu category đã tồn tại và chưa bị xóa
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Category already exists.",
                        data = null
                    };
                }
            }

            // Nếu không tìm thấy category đã tồn tại, tiến hành tạo mới
            var category = new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = request.CategoryName,
                Description = request.Description,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                ModifyDate = TimeUtils.GetCurrentSEATime(),
                IsDelete = false,
            };

            // Thêm mới category vào database
            await _unitOfWork.GetRepository<Category>().InsertAsync(category);
            int isSuccessfulCreate = await _unitOfWork.CommitAsync();  // Commit thay đổi

            if (isSuccessfulCreate <= 0)  // Kiểm tra có thành công không
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Failed to create category.",
                    data = null
                };
            }

            var response = new CategoryResponse
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                Description = category.Description,
                CreateDate = category.CreateDate,
                ModifyDate = category.ModifyDate,
            };

            return new ApiResponse
            {
                status = StatusCodes.Status201Created.ToString(),
                message = "Category created successfully.",
                data = response
            };
        }


        public async Task<ApiResponse> GetAllCategory(int page, int size, string searchName, bool? isAscending)
        {
            var categories = await _unitOfWork.GetRepository<Category>().GetPagingListAsync(
                selector: c => new CategoryResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    CreateDate = c.CreateDate,
                    ModifyDate = c.ModifyDate,
                    // Dữ liệu SubCategory sẽ được ánh xạ tại đây
                    SubCategories = c.SubCategories
                        .Where(sub => sub.IsDelete != true)  // Lọc nếu cần
                        .Select(sub => new SubCategoryResponse
                        {
                            Id = sub.Id,
                            SubCategoryName = sub.SubCategoryName,
                            CategoryId = sub.CategoryId,
                            Description = sub.Description,
                            CreateDate = sub.CreateDate,
                            ModifyDate = sub.ModifyDate,
                        }).ToList()
                },
                predicate: p => p.IsDelete.Equals(false) &&
                                (string.IsNullOrEmpty(searchName) || p.CategoryName.Contains(searchName)),
                orderBy: q => isAscending.HasValue
                    ? (isAscending.Value ? q.OrderBy(p => p.CategoryName) : q.OrderByDescending(p => p.CategoryName))
                    : q.OrderByDescending(p => p.CreateDate),
                size: size,
                page: page);

            int totalItems = categories.Total;
            int totalPages = (int)Math.Ceiling((double)totalItems / size);

            if (categories == null || categories.Items.Count == 0)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Category retrieved successfully.",
                    data = new Paginate<Category>()
                    {
                        Page = page,
                        Size = size,
                        Total = totalItems,
                        TotalPages = totalPages,
                        Items = new List<Category>()
                    }
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Categories retrieved successfully.",
                data = categories
            };
        }


        public async Task<ApiResponse> GetCategory(Guid id)
        {
            var category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                selector: c => new CategoryResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    CreateDate = c.CreateDate,
                    ModifyDate = c.ModifyDate,
                    SubCategories = c.SubCategories
                        .Where(sub => sub.IsDelete != true)  // Lọc nếu cần
                        .Select(sub => new SubCategoryResponse
                        {
                            Id = sub.Id,
                            SubCategoryName = sub.SubCategoryName,
                            CategoryId = sub.CategoryId,
                            Description = sub.Description,
                            CreateDate = sub.CreateDate,
                            ModifyDate = sub.ModifyDate,
                        }).ToList()
                },
                predicate: c => c.Id.Equals(id) &&
                                c.IsDelete.Equals(false));

            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = MessageConstant.CategoryMessage.CategoryNotExist,
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Category retrieved successfully.",
                data = category
            };
        }

        public async Task<ApiResponse> UpdateCategory(Guid id, CategoryRequest request)
        {
            var category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: c => c.Id.Equals(id) &&
                                c.IsDelete.Equals(false));

            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = MessageConstant.CategoryMessage.CategoryNotExist,
                    data = null
                };
            }

            // Check if CategoryName already exists in the database
            var existingCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: c => c.CategoryName.Equals(request.CategoryName) &&
                                !c.Id.Equals(id) &&
                                c.IsDelete.Equals(false));

            if (existingCategory != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "CategoryName has exist!",
                    data = null
                };
            }

            category.CategoryName = string.IsNullOrEmpty(request.CategoryName)
                ? category.CategoryName
                : request.CategoryName;
            category.ModifyDate = TimeUtils.GetCurrentSEATime();
            category.Description = string.IsNullOrEmpty(request.Description)
                ? category.Description
                : request.Description;
            _unitOfWork.GetRepository<Category>().UpdateAsync(category);

            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Failed to update category.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Category updated successfully.",
                data = true
            };
        }

        public async Task<ApiResponse> DeleteCategory(Guid id)
        {
            // Kiểm tra sự tồn tại của Category
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == false);

            // Nếu không tìm thấy Category
            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Category không tồn tại hoặc đã bị xóa.",
                    data = null
                };
            }

            // Cập nhật IsDelete thành true
            category.IsDelete = true;
            _unitOfWork.GetRepository<Category>().UpdateAsync(category);

            // Lưu thay đổi
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            // Trả về kết quả
            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể xóa Category.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Category đã được xóa thành công.",
                data = null
            };
        }
    }
}
