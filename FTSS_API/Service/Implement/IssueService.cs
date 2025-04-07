using System.Linq.Expressions;
using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.Issue;
using FTSS_API.Payload.Response.SetupPackage;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
using LinqKit;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

public class IssueService : BaseService<IssueService>, IIssueService
{
    public IssueService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<IssueService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
        : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
    }

    public async Task<ApiResponse> CreateIssue(AddUpdateIssueRequest request)
    {
        // Create the Issue without including Solutions in the mapping
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IssueCategoryId = request.IssueCategoryId,
            CreateDate = DateTime.UtcNow,
            IsDelete = false
            // Don't map Solutions here
        };

        // Add Issue to Database
        await _unitOfWork.GetRepository<Issue>().InsertAsync(issue);

        // Process solutions and products list
        if (request.Solutions != null && request.Solutions.Any())
        {
            var solutions = new List<Solution>();
            var solutionProducts = new List<SolutionProduct>();

            foreach (var solutionRequest in request.Solutions)
            {
                // Create new solution
                var solution = new Solution
                {
                    Id = Guid.NewGuid(),
                    SolutionName = solutionRequest.SolutionName,
                    Description = solutionRequest.Description,
                    IssueId = issue.Id,
                    CreateDate = DateTime.UtcNow,
                    IsDelete = false
                };
                solutions.Add(solution);

                // Add related products
                if (solutionRequest.ProductIds != null && solutionRequest.ProductIds.Any())
                {
                    foreach (var productId in solutionRequest.ProductIds)
                    {
                        solutionProducts.Add(new SolutionProduct
                        {
                            Id = Guid.NewGuid(),
                            SolutionId = solution.Id,
                            ProductId = productId,
                            CreateDate = DateTime.UtcNow
                        });
                    }
                }
            }

            // Add solutions and solutionProducts to database
            await _unitOfWork.GetRepository<Solution>().InsertRangeAsync(solutions);
            if (solutionProducts.Any())
            {
                await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(solutionProducts);
            }
        }

        // Commit transaction
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        Issue createdIssue = null;
        if (isSuccessful)
        {
            createdIssue = await _unitOfWork.GetRepository<Issue>()
                .SingleOrDefaultAsync(
                    predicate: i => i.Id == issue.Id,
                    include: source => source
                        .Include(i => i.IssueCategory)
                        .Include(i => i.Solutions)
                        .ThenInclude(s => s.SolutionProducts)
                        .ThenInclude(sp => sp.Product)
                        .ThenInclude(s => s.Images)
                );
        }

        // Return the response
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issue created successfully with solutions and related products.",
            data = _mapper.Map<IssueResponse>(createdIssue)
        };
    }

    public async Task<ApiResponse> GetAllIssues(int page, int size, bool? isAscending, Guid? issueCategoryId = null, string issueTitle = null)
    {
        Expression<Func<Issue, bool>> predicate = i => i.IsDelete == false;

        if (issueCategoryId.HasValue)
        {
            predicate = predicate.And(i => i.IssueCategoryId == issueCategoryId.Value);
        }

        // Lọc theo tên của IssueCategory
        if (!string.IsNullOrEmpty(issueTitle))
        {
            predicate = predicate.And(i => i.Title.Contains(issueTitle));
        }

        Func<IQueryable<Issue>, IOrderedQueryable<Issue>> orderBy = null;
        if (isAscending.HasValue)
        {
            orderBy = isAscending.Value
                ? q => q.OrderBy(i => i.CreateDate)
                : q => q.OrderByDescending(i => i.CreateDate);
        }

        var issues = await _unitOfWork.GetRepository<Issue>()
            .GetPagingListAsync(
                predicate,
                orderBy,
                include: i => i
                    .Include(i => i.IssueCategory) // ✅ Cần include để filter theo Name
                    .Include(i => i.Solutions.Where(s => s.IsDelete == false))
                    .ThenInclude(s => s.SolutionProducts)
                    .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.Images),
                page,
                size);

        var mapped = _mapper.Map<List<IssueResponse>>(issues.Items);

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Lấy danh sách vấn đề thành công",
            data = mapped
        };
    }

    public async Task<ApiResponse> GetIssueById(Guid id)
    {
        var issue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == false,
                include: source => source
                    .Include(i => i.IssueCategory)
                    .Include(i => i.Solutions.Where(s => s.IsDelete == false))
                    .ThenInclude(s => s.SolutionProducts)
                    .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.Images)
            );

        if (issue == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Issue not found."
            };
        }

        var mappedIssue = _mapper.Map<IssueResponse>(issue);

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Fetched issue successfully.",
            data = mappedIssue
        };
    }

 public async Task<ApiResponse> UpdateIssue(Guid id, AddUpdateIssueRequest request)
{
    // var existingIssue = await _unitOfWork.GetRepository<Issue>()
    //     .Where(i => i.Id == id && i.IsDelete == false)
    //     .Include(i => i.Solutions)
    //         .ThenInclude(s => s.Products)  // Include Products for each Solution
    //     .Include(i => i.Category) // Include Issue Category (for IssueCategoryName)
    //     .FirstOrDefaultAsync();
    var existingIssue = await _unitOfWork.GetRepository<Issue>()
        .SingleOrDefaultAsync(
            predicate: i => i.Id == id && i.IsDelete == false,
            include: i => i.Include(i => i.Solutions)
                .ThenInclude(s => s.SolutionProducts)
                .Include(i => i.IssueCategory)
        );

    if (existingIssue == null)
        return new ApiResponse { status = StatusCodes.Status404NotFound.ToString(), message = "Issue not found." };

    // Update issue properties
    _mapper.Map(request, existingIssue);
    existingIssue.ModifiedDate = DateTime.UtcNow;

    // Handle solutions update
    if (request.Solutions != null)
    {
        // Get existing solutions
        var existingSolutions = existingIssue.Solutions?.ToList() ?? new List<Solution>();

        // Process solutions to add or update
        foreach (var solutionRequest in request.Solutions)
        {
            var existingSolution = existingSolutions.FirstOrDefault(s => s.Id == solutionRequest.Id);

            if (existingSolution != null)
            {
                // Update existing solution
                _mapper.Map(solutionRequest, existingSolution);
                existingSolution.ModifiedDate = DateTime.UtcNow;
                _unitOfWork.GetRepository<Solution>().UpdateAsync(existingSolution);

                // Handle solution products update
                await UpdateSolutionProducts(existingSolution.Id, solutionRequest.ProductIds);
            }
            else
            {
                // Add new solution
                var newSolution = _mapper.Map<Solution>(solutionRequest);
                newSolution.Id = Guid.NewGuid();
                newSolution.IssueId = existingIssue.Id;
                newSolution.CreateDate = DateTime.UtcNow;
                newSolution.IsDelete = false;

                await _unitOfWork.GetRepository<Solution>().InsertAsync(newSolution);

                // Add solution products
                if (solutionRequest.ProductIds != null && solutionRequest.ProductIds.Any())
                {
                    var solutionProducts = solutionRequest.ProductIds.Select(productId => new SolutionProduct
                    {
                        Id = Guid.NewGuid(),
                        SolutionId = newSolution.Id,
                        ProductId = productId,
                        CreateDate = DateTime.UtcNow
                    }).ToList();

                    await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(solutionProducts);
                }
            }
        }

        // Handle solutions to delete (soft delete)
        var solutionIdsToKeep = request.Solutions.Where(s => s.Id.HasValue).Select(s => s.Id.Value).ToList();
        var solutionsToDelete = existingSolutions.Where(s => !solutionIdsToKeep.Contains(s.Id)).ToList();

        foreach (var solutionToDelete in solutionsToDelete)
        {
            solutionToDelete.IsDelete = true;
            solutionToDelete.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<Solution>().UpdateAsync(solutionToDelete);
        }
    }

    bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

    // Create the response
    var issueResponse = _mapper.Map<IssueResponse>(existingIssue);

    // Add IssueCategoryName
    issueResponse.IssueCategoryName = existingIssue.IssueCategory?.IssueCategoryName;

    // Add Products for each Solution
    foreach (var solution in issueResponse.Solutions)
    {
        solution.Products = existingIssue.Solutions
            .FirstOrDefault(s => s.Id == solution.Id)?
            .SolutionProducts?.Select(p => new IssueProductResponse() // Changed from ProductResponse to IssueProductResponse
            {
                ProductId = p.ProductId,
                ProductName = p.Product?.ProductName,
                ProductImageUrl = p.Product.Images.FirstOrDefault()?.LinkImage,
            }).ToList();
    }

    return new ApiResponse
    {
        status = isSuccessful
            ? StatusCodes.Status200OK.ToString()
            : StatusCodes.Status500InternalServerError.ToString(),
        message = isSuccessful
            ? "Issue updated successfully with solutions and products."
            : "Failed to update Issue.",
        data = issueResponse
    };
}

    private async Task UpdateSolutionProducts(Guid solutionId, List<Guid>? newProductIds)
    {
        if (newProductIds == null) return;

        var existingSolutionProducts = await _unitOfWork.GetRepository<SolutionProduct>()
            .GetListAsync(predicate: sp => sp.SolutionId == solutionId);

        // Get existing product IDs
        var existingProductIds = existingSolutionProducts.Select(sp => sp.ProductId).ToList();

        // Products to add
        var productsToAdd = newProductIds.Except(existingProductIds)
            .Select(productId => new SolutionProduct
            {
                Id = Guid.NewGuid(),
                SolutionId = solutionId,
                ProductId = productId,
                CreateDate = DateTime.UtcNow
            }).ToList();

        if (productsToAdd.Any())
        {
            await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(productsToAdd);
        }

        // Products to remove
        var productsToRemove = existingSolutionProducts
            .Where(sp => !newProductIds.Contains(sp.ProductId))
            .ToList();

        if (productsToRemove.Any())
        {
            _unitOfWork.GetRepository<SolutionProduct>().DeleteRangeAsync(productsToRemove);
        }
    }

    public async Task<ApiResponse> DeleteIssue(Guid id)
    {
        var issue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == false,
                include: i => i.Include(i => i.Solutions));

        if (issue == null)
            return new ApiResponse { status = StatusCodes.Status404NotFound.ToString(), message = "Issue not found." };

        // Soft delete issue
        issue.IsDelete = true;
        issue.ModifiedDate = DateTime.UtcNow;
        _unitOfWork.GetRepository<Issue>().UpdateAsync(issue);

        // Soft delete related solutions
        if (issue.Solutions != null)
        {
            foreach (var solution in issue.Solutions)
            {
                solution.IsDelete = true;
                solution.ModifiedDate = DateTime.UtcNow;
                _unitOfWork.GetRepository<Solution>().UpdateAsync(solution);
            }
        }

        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isSuccessful
                ? StatusCodes.Status200OK.ToString()
                : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue and related solutions deleted successfully." : "Failed to delete Issue."
        };
    }
}