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
using Google.Apis.Drive.v3;
using Microsoft.IdentityModel.Tokens;
using MRC_API.Utils;
using Microsoft.EntityFrameworkCore;


namespace FTSS_API.Service.Implement
{
    public class CategoryService : BaseService<CategoryService>, ICategoryService
    {
        private readonly SupabaseUltils _supabaseImageService;
        private readonly HtmlSanitizerUtils _sanitizer;
        public CategoryService(IUnitOfWork<MyDbContext> unitOfWork,
            ILogger<CategoryService> logger,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            SupabaseUltils supabaseImageService,
            HtmlSanitizerUtils htmlSanitizer
            ) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _sanitizer = htmlSanitizer;
            _supabaseImageService = supabaseImageService;
        }

        public async Task<ApiResponse> CreateCategory(CategoryRequest request, Supabase.Client client)
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

            // Check if category already exists
            var categoryExist = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: c => c.CategoryName.Equals(request.CategoryName) && c.IsDelete == false);

            if (categoryExist != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Category already exists.",
                    data = null
                };
            }
            // Check if image is provided
            if (request.ImageFile == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Image file is required.",
                    data = null
                };
            }

            string? imageUrl = null;

            // Upload image if provided
            if (request.ImageFile != null)
            {
                try
                {
                    // Call your Supabase image upload service
                    imageUrl = (await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.ImageFile }, client)).FirstOrDefault();

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = "Failed to upload image.",
                            data = null
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status500InternalServerError.ToString(),
                        message = $"An error occurred while uploading the image: {ex.Message}",
                        data = null
                    };
                }
            }

            // Create category object
            var category = new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = request.CategoryName,
                Description = request.Description,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                ModifyDate = TimeUtils.GetCurrentSEATime(),
                IsDelete = false,
                IsFishTank = request.IsFishTank,
                IsObligatory = request.IsObligatory,
                IsSolution = request.IsSolution,
                LinkImage = imageUrl // Save the image URL
            };

            try
            {
                // Insert category into database
                await _unitOfWork.GetRepository<Category>().InsertAsync(category);
                bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

                if (isSuccessful)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status201Created.ToString(),
                        message = "Category created successfully.",
                        data = new
                        {
                            category.Id,
                            category.CategoryName,
                            category.Description,
                            category.LinkImage,
                            category.CreateDate,
                            category.ModifyDate,
                            category.IsFishTank,
                            category.IsObligatory,
                            category.IsSolution,
                        }
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status500InternalServerError.ToString(),
                        message = "Failed to create category.",
                        data = null
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"An error occurred: {ex.Message}",
                    data = null
                };
            }
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
                    IsFishTank = c.IsFishTank,
                    IsObligatory = c.IsObligatory,
                    IsSolution = c.IsSolution,
                    IsDelete = c.IsDelete,
                    LinkImage = c.LinkImage,
                    // Dữ liệu SubCategory sẽ được ánh xạ tại đây
                    SubCategories = c.SubCategories
                          // Lọc nếu cần
                        .Select(sub => new SubCategoryResponse
                        {
                            Id = sub.Id,
                            SubCategoryName = sub.SubCategoryName,
                            CategoryId = sub.CategoryId,
                            Description = sub.Description,
                            CreateDate = sub.CreateDate,
                            ModifyDate = sub.ModifyDate,
                            CategoryName = sub.Category.CategoryName,
                            IsDelete = sub.IsDelete,
                        }).ToList()
                },
                predicate: p => 
                                (string.IsNullOrEmpty(searchName) || p.CategoryName.Contains(searchName)),
                orderBy: q =>
                {
                    var ordered = q.OrderByDescending(p => p.IsFishTank); // Ưu tiên IsFishTank = true lên đầu
                    if (isAscending.HasValue)
                    {
                        ordered = isAscending.Value
                            ? ordered.ThenBy(p => p.CategoryName)
                            : ordered.ThenByDescending(p => p.CategoryName);
                    }
                    else
                    {
                        ordered = ordered.ThenByDescending(p => p.CreateDate);
                    }

                    return ordered;
                },
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
                    IsObligatory = c.IsObligatory,
                    IsFishTank = c.IsFishTank,
                    IsSolution = c.IsSolution,
                    IsDelete = c.IsDelete,
                    LinkImage = c.LinkImage,
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
                            IsDelete= sub.IsDelete,
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

        public async Task<ApiResponse> UpdateCategory(Guid categoryId, CategoryRequest updateCategoryRequest, Supabase.Client client)
        {
            // Lấy thông tin danh mục từ database
            var existingCategory = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: c => c.Id.Equals(categoryId) && (c.IsDelete == null || c.IsDelete == false));

            if (existingCategory == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = MessageConstant.CategoryMessage.CategoryNotExist,
                    data = null
                };
            }

            // Kiểm tra tên danh mục nếu có thay đổi
            if (!string.IsNullOrEmpty(updateCategoryRequest.CategoryName) &&
                !existingCategory.CategoryName.Equals(updateCategoryRequest.CategoryName))
            {
                var categoryCheck = await _unitOfWork.GetRepository<Category>()
                    .SingleOrDefaultAsync(predicate: c => c.CategoryName.Equals(updateCategoryRequest.CategoryName) && (c.IsDelete == null || c.IsDelete == false));
                if (categoryCheck != null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "CategoryName already exists!",
                        data = null
                    };
                }

                existingCategory.CategoryName = updateCategoryRequest.CategoryName;
            }

            // Cập nhật mô tả nếu có
            if (!string.IsNullOrEmpty(updateCategoryRequest.Description))
            {
                existingCategory.Description = updateCategoryRequest.Description;
            }

            // Cập nhật IsFishTank nếu có
            if (updateCategoryRequest.IsFishTank.HasValue)
            {
                existingCategory.IsFishTank = updateCategoryRequest.IsFishTank;
            }

            // Cập nhật IsObligatory nếu có
            if (updateCategoryRequest.IsObligatory.HasValue)
            {
                existingCategory.IsObligatory = updateCategoryRequest.IsObligatory;
            }
            if (updateCategoryRequest.IsSolution.HasValue)
            {
                existingCategory.IsSolution = updateCategoryRequest.IsSolution;
            }

            // Cập nhật hình ảnh nếu có
            if (updateCategoryRequest.ImageFile != null)
            {
                try
                {
                    // Upload hình ảnh mới lên Supabase
                    var imageUrls = await _supabaseImageService.SendImagesAsync(new List<IFormFile> { updateCategoryRequest.ImageFile }, client);

                    if (imageUrls != null && imageUrls.Any())
                    {
                        existingCategory.LinkImage = imageUrls.FirstOrDefault();
                    }
                    else
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = "Failed to upload new image.",
                            data = null
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status500InternalServerError.ToString(),
                        message = $"An error occurred while uploading the image: {ex.Message}",
                        data = null
                    };
                }
            }

            // Cập nhật ngày sửa
            existingCategory.ModifyDate = TimeUtils.GetCurrentSEATime();

            // Gửi yêu cầu cập nhật danh mục
            _unitOfWork.GetRepository<Category>().UpdateAsync(existingCategory);
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
                data = new
                {
                    existingCategory.Id,
                    existingCategory.CategoryName,
                    existingCategory.Description,
                    existingCategory.LinkImage,
                    existingCategory.IsFishTank,
                    existingCategory.IsObligatory,
                    existingCategory.IsSolution,
                    existingCategory.ModifyDate
                }
            };
        }



        public async Task<ApiResponse> DeleteCategory(Guid id)
        {
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(
                    predicate: c => c.Id == id && c.IsDelete == false,
                    include: source => source.Include(c => c.SubCategories));

            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Category không tồn tại hoặc đã bị xóa.",
                    data = null
                };
            }

            // Cách 1: Dùng GetQueryable để kiểm tra sản phẩm còn sống
            var productExists = await _unitOfWork.GetRepository<Product>()
    .GetQueryable()
    .Where(p => p.IsDelete == false &&
                p.SubCategory != null &&
                p.SubCategory.CategoryId == id)
    .AnyAsync();

            if (productExists)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Không thể xóa Category vì đang chứa sản phẩm còn hoạt động.",
                    data = null
                };
            }

            category.IsDelete = true;
            _unitOfWork.GetRepository<Category>().UpdateAsync(category);

            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

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

        public async Task<ApiResponse> GetListCategory(int page, int size, string? searchName, bool? isAscending)
        {
            var categories = await _unitOfWork.GetRepository<Category>().GetPagingListAsync(
                selector: c => new CategoryResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    CreateDate = c.CreateDate,
                    ModifyDate = c.ModifyDate,
                    IsFishTank = c.IsFishTank,
                    IsObligatory = c.IsObligatory,
                    IsSolution = c.IsSolution,
                    LinkImage = c.LinkImage,
                    IsDelete = c.IsDelete,
                    SubCategories = c.SubCategories
                        .Where(sub => sub.IsDelete != true)
                        .Select(sub => new SubCategoryResponse
                        {
                            Id = sub.Id,
                            SubCategoryName = sub.SubCategoryName,
                            CategoryId = sub.CategoryId,
                            Description = sub.Description,
                            CreateDate = sub.CreateDate,
                            ModifyDate = sub.ModifyDate,
                            CategoryName = sub.Category.CategoryName,
                            IsDelete = sub.IsDelete,
                        }).ToList()
                },
                predicate: c => c.IsDelete != true &&
                                c.IsSolution == false &&
                                (string.IsNullOrEmpty(searchName) || c.CategoryName.Contains(searchName)),
                orderBy: q =>
                {
                    var ordered = q.OrderByDescending(c => c.IsFishTank); // Ưu tiên IsFishTank = true

                    if (isAscending.HasValue)
                    {
                        ordered = isAscending.Value
                            ? ordered.ThenBy(c => c.CategoryName)
                            : ordered.ThenByDescending(c => c.CategoryName);
                    }
                    else
                    {
                        ordered = ordered.ThenByDescending(c => c.CreateDate);
                    }

                    return ordered;
                },
                size: size,
                page: page);

            int totalItems = categories.Total;
            int totalPages = (int)Math.Ceiling((double)totalItems / size);

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Categories retrieved successfully.",
                data = new Paginate<CategoryResponse>
                {
                    Page = page,
                    Size = size,
                    Total = totalItems,
                    TotalPages = totalPages,
                    Items = categories.Items
                }
            };
        }

        public async Task<ApiResponse> EnableCategory(Guid id)
        {
            // Kiểm tra sự tồn tại của Category đã bị xóa
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == true);

            // Nếu không tìm thấy Category
            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Category không tồn tại hoặc chưa bị xóa.",
                    data = null
                };
            }

            // Cập nhật IsDelete thành false
            category.IsDelete = false;
            _unitOfWork.GetRepository<Category>().UpdateAsync(category);

            // Lưu thay đổi
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            // Trả về kết quả
            if (!isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Không thể kích hoạt lại Category.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Category đã được kích hoạt lại thành công.",
                data = null
            };
        }
    }
}
