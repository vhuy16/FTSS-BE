using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Controller;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Category;
using FTSS_API.Payload.Response.Category;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
using FTSS_Repository.Interface;
using static FTSS_API.Constant.ApiEndPointConstant;

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

            var categoryExist = await _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().SingleOrDefaultAsync(
                predicate: c => c.CategoryName.Equals(request.CategoryName));

            if (categoryExist != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = MessageConstant.CategoryMessage.CategoryExisted,
                    data = null
                };
            }

            FTSS_Model.Entities.Category category = new FTSS_Model.Entities.Category
            {
                Id = Guid.NewGuid(),
                CategoryName = request.CategoryName,
                Description = request.Description,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                ModifyDate = TimeUtils.GetCurrentSEATime(),
                IsDelete = false,
            };

            await _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().InsertAsync(category);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (!isSuccessful)
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
            var categories = await _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().GetPagingListAsync(
                selector: c => new CategoryResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description= c.Description,
                    CreateDate = c.CreateDate,
                    ModifyDate = c.ModifyDate,
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
                    data = new Paginate<FTSS_Model.Entities.Category>()
                    {
                        Page = page,
                        Size = size,
                        Total = totalItems,
                        TotalPages = totalPages,
                        Items = new List<FTSS_Model.Entities.Category>()
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
            var category = await _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().SingleOrDefaultAsync(
                selector: c => new CategoryResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    CreateDate = c.CreateDate,
                    ModifyDate = c.ModifyDate
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
            var category = await _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().SingleOrDefaultAsync(
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
            var existingCategory = await _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().SingleOrDefaultAsync(
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
            _unitOfWork.GetRepository<FTSS_Model.Entities.Category>().UpdateAsync(category);

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

        //public async Task<ApiResponse> DeleteCategory(Guid id)
        //{
        //    var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        //    if (category == null)
        //    {
        //        return new ApiResponse
        //        {
        //            status = "404",
        //            message = "Category not found"
        //        };
        //    }

        //    _unitOfWork.Repository<Category>().Delete(category);
        //    await _unitOfWork.CommitAsync();

        //    return new ApiResponse
        //    {
        //        status = "200",
        //        message = "Category deleted successfully"
        //    };
        //}
    }
}
