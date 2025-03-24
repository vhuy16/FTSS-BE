using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.IssueCategory;
using FTSS_API.Payload.Response.IssueCategory;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Repository.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FTSS_API.Service.Implement
{
    public class IssueCategoryService : BaseService<IssueCategoryService>, IIssueCategoryService
    {
        public IssueCategoryService(IUnitOfWork<MyDbContext> unitOfWork,
            ILogger<IssueCategoryService> logger,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }
        public async Task<ApiResponse> CreateIssueCategory(AddUpdateIssueCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IssueCategoryName))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "IssueCategoryName cannot be empty.",
                    data = null
                };
            }

            var categoryExist = await _unitOfWork.GetRepository<IssueCategory>()
                .SingleOrDefaultAsync(predicate:c => c.IssueCategoryName.Equals(request.IssueCategoryName) && c.IsDelete == false);

            if (categoryExist != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "IssueCategory already exists.",
                    data = null
                };
            }

            var issueCategory = _mapper.Map<IssueCategory>(request);
            issueCategory.Id = Guid.NewGuid();
            issueCategory.CreateDate = DateTime.UtcNow;
            issueCategory.IsDelete = false;

            await _unitOfWork.GetRepository<IssueCategory>().InsertAsync(issueCategory);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse
            {
                status = isSuccessful ? StatusCodes.Status201Created.ToString() : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "IssueCategory created successfully." : "Failed to create IssueCategory.",
                data = _mapper.Map<IssueCategoryResponse>(issueCategory)
            };
        }


        public async Task<ApiResponse> GetAllIssueCategories()
        {
            var categories = await _unitOfWork.GetRepository<IssueCategory>().GetListAsync(predicate: c => c.IsDelete == false);
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "IssueCategories retrieved successfully.",
                data = categories
            };
        }

        public async Task<ApiResponse> GetIssueCategory(Guid id)
        {
            var category = await _unitOfWork.GetRepository<IssueCategory>().SingleOrDefaultAsync(predicate: c => c.Id.Equals(id) && c.IsDelete == false);
            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "IssueCategory not found.",
                    data = null
                };
            }
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "IssueCategory retrieved successfully.",
                data = category
            };
        }

        public async Task<ApiResponse> UpdateIssueCategory(Guid id, AddUpdateIssueCategoryRequest request)
        {
            var existingCategory = await _unitOfWork.GetRepository<IssueCategory>().SingleOrDefaultAsync(
                predicate:c => c.Id.Equals(id) && c.IsDelete == false);

            if (existingCategory == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "IssueCategory not found.",
                    data = null
                };
            }

            _mapper.Map(request, existingCategory);
            existingCategory.ModifyDate = DateTime.UtcNow;

            _unitOfWork.GetRepository<IssueCategory>().UpdateAsync(existingCategory);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse
            {
                status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "IssueCategory updated successfully." : "Failed to update IssueCategory.",
                data = _mapper.Map<IssueCategoryResponse>(existingCategory)
            };
        }

        public async Task<ApiResponse> DeleteIssueCategory(Guid id)
        {
            var category = await _unitOfWork.GetRepository<IssueCategory>().SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == false);
            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "IssueCategory not found.",
                    data = null
                };
            }

            category.IsDelete = true;
            _unitOfWork.GetRepository<IssueCategory>().UpdateAsync(category);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse
            {
                status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "IssueCategory deleted successfully." : "Failed to delete IssueCategory.",
                data = null
            };
        }
    }
}