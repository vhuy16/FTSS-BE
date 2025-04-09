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
                .SingleOrDefaultAsync(predicate: c =>
                    c.IssueCategoryName.Equals(request.IssueCategoryName) && c.IsDelete == false);

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
                status = isSuccessful
                    ? StatusCodes.Status201Created.ToString()
                    : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "IssueCategory created successfully." : "Failed to create IssueCategory.",
                data = _mapper.Map<IssueCategoryResponse>(issueCategory)
            };
        }


        public async Task<ApiResponse> GetAllIssueCategories()
        {
            var categories = await _unitOfWork.GetRepository<IssueCategory>()
                .GetListAsync(predicate: c => c.IsDelete == false);
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "IssueCategories retrieved successfully.",
                data = categories
            };
        }

        public async Task<ApiResponse> GetIssueCategory(Guid id)
        {
            var category = await _unitOfWork.GetRepository<IssueCategory>()
                .SingleOrDefaultAsync(predicate: c => c.Id.Equals(id) && c.IsDelete == false);
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
                predicate: c => c.Id.Equals(id) && c.IsDelete == false);

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
                status = isSuccessful
                    ? StatusCodes.Status200OK.ToString()
                    : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "IssueCategory updated successfully." : "Failed to update IssueCategory.",
                data = _mapper.Map<IssueCategoryResponse>(existingCategory)
            };
        }

        public async Task<ApiResponse> DeleteIssueCategory(Guid id)
        {
            // Lấy IssueCategory cần xóa
            var category = await _unitOfWork.GetRepository<IssueCategory>()
                .SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == false);

            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Không tìm thấy danh mục vấn đề.",
                    data = null
                };
            }

            // Kiểm tra xem IssueCategory có đang được sử dụng bởi bất kỳ Issue nào không
            var issuesUsingCategory = await _unitOfWork.GetRepository<Issue>()
                .GetListAsync(predicate: i => i.IssueCategoryId == id && i.IsDelete == false);

            if (issuesUsingCategory.Any())
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Không thể xóa danh mục vấn đề vì danh mục này đang được sử dụng bởi các vấn đề khác.",
                    data = null
                };
            }

            // Thực hiện soft delete cho IssueCategory
            category.IsDelete = true;
            category.ModifyDate = DateTime.UtcNow; // Thêm ModifiedDate để ghi nhận thời gian thay đổi
            _unitOfWork.GetRepository<IssueCategory>().UpdateAsync(category);

            // Lưu thay đổi vào cơ sở dữ liệu
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse
            {
                status = isSuccessful
                    ? StatusCodes.Status200OK.ToString()
                    : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "Danh mục vấn đề đã được xóa thành công." : "Xóa danh mục vấn đề thất bại.",
                data = null
            };
        }
        public async Task<ApiResponse> EnableIssueCategory(Guid id)
        {
            // Lấy IssueCategory cần kích hoạt
            var category = await _unitOfWork.GetRepository<IssueCategory>()
                .SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == true);

            if (category == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Không tìm thấy danh mục vấn đề hoặc danh mục chưa bị xóa.",
                    data = null
                };
            }

            // Kích hoạt lại IssueCategory
            category.IsDelete = false;
            category.ModifyDate = DateTime.UtcNow; // Cập nhật thời gian chỉnh sửa
            _unitOfWork.GetRepository<IssueCategory>().UpdateAsync(category);

            // Lưu thay đổi vào cơ sở dữ liệu
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse
            {
                status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessful ? "Danh mục vấn đề đã được kích hoạt lại thành công." : "Kích hoạt lại danh mục vấn đề thất bại.",
                data = null
            };
        }
    }
}