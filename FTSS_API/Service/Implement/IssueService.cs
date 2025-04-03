using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.Issue;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

public class IssueService : BaseService<IssueService>, IIssueService
{
    public IssueService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<IssueService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        : base(unitOfWork, logger, mapper, httpContextAccessor) {}

    public async Task<ApiResponse> CreateIssue(AddUpdateIssueRequest request)
    {
        var issue = _mapper.Map<Issue>(request);
        issue.Id = Guid.NewGuid();
        issue.CreateDate = DateTime.UtcNow;
        issue.IsDelete = false;

        // Thêm Issue vào Database
        await _unitOfWork.GetRepository<Issue>().InsertAsync(issue);
    
        // Xử lý danh sách sản phẩm liên quan
        if (request.ProductIds != null && request.ProductIds.Any())
        {
            var issueProducts = request.ProductIds.Select(productId => new IssueProduct
            {
                Id = Guid.NewGuid(),
                IssueId = issue.Id,
                ProductId = productId,
                CreateDate = DateTime.UtcNow
            }).ToList();

            await _unitOfWork.GetRepository<IssueProduct>().InsertRangeAsync(issueProducts);
        }

        // Commit transaction
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isSuccessful ? StatusCodes.Status201Created.ToString() : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue created successfully with related products." : "Failed to create Issue.",
            data = _mapper.Map<IssueResponse>(issue)
        };
    }


    public async Task<ApiResponse> GetAllIssues()
    {
        var issues = await _unitOfWork.GetRepository<Issue>().GetListAsync(predicate: i => i.IsDelete == false);
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issues retrieved successfully.",
            data = issues
        };
    }

    public async Task<ApiResponse> GetIssue(Guid id)
    {
        var issue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(predicate: c => c.Id == id && c.IsDelete == false, include: i => i.Include(i => i.IssueProducts));

        if (issue == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Issue not found.",
                data = null
            };
        }

        // Map danh sách sản phẩm liên quan
        var response = _mapper.Map<IssueResponse>(issue);
        response.RelatedProducts = issue.IssueProducts?.Select(ip => new IssueProductResponse
        {
            ProductId = ip.Product.Id,
            ProductName = ip.Product.ProductName,
            Description = ip.Product.Description
        }).ToList();

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issue retrieved successfully.",
            data = response
        };
    }

    public async Task<ApiResponse> UpdateIssue(Guid id, AddUpdateIssueRequest request)
    {
        var existingIssue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(predicate:i => i.Id == id && i.IsDelete == false);
        if (existingIssue == null)
            return new ApiResponse { status = StatusCodes.Status404NotFound.ToString(), message = "Issue not found." };

        _mapper.Map(request, existingIssue);
        existingIssue.CreateDate = DateTime.UtcNow;

        _unitOfWork.GetRepository<Issue>().UpdateAsync(existingIssue);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue updated successfully." : "Failed to update Issue.",
            data = _mapper.Map<IssueResponse>(existingIssue)
        };
    }

    public async Task<ApiResponse> DeleteIssue(Guid id)
    {
        var issue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(predicate:i => i.Id == id && i.IsDelete == false);
        if (issue == null)
            return new ApiResponse { status = StatusCodes.Status404NotFound.ToString(), message = "Issue not found." };

        issue.IsDelete = true;
        _unitOfWork.GetRepository<Issue>().UpdateAsync(issue);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue deleted successfully." : "Failed to delete Issue."
        };
    }
}
