using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.Issue;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
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

        // Xử lý danh sách solutions và products
        if (request.Solutions != null && request.Solutions.Any())
        {
            var solutions = new List<Solution>();
            var solutionProducts = new List<SolutionProduct>();

            foreach (var solutionRequest in request.Solutions)
            {
                // Tạo solution mới
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

                // Thêm các sản phẩm liên quan đến solution
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

            // Thêm solutions và solutionProducts vào database
            await _unitOfWork.GetRepository<Solution>().InsertRangeAsync(solutions);
            if (solutionProducts.Any())
            {
                await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(solutionProducts);
            }
        }

        // Commit transaction
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isSuccessful ? StatusCodes.Status201Created.ToString() : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue created successfully with solutions and related products." : "Failed to create Issue.",
            data = _mapper.Map<IssueResponse>(issue)
        };
    }

    public async Task<ApiResponse> GetAllIssues(int page, int size, bool? isAscending, Guid? issueCategoryId = null)
    {
        var issues = await _unitOfWork.GetRepository<Issue>().GetPagingListAsync(
            selector: i => new IssueResponse
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                IssueCategoryId = i.IssueCategoryId,
                IssueCategoryName = i.IssueCategory.IssueCategoryName,
                CreateDate = i.CreateDate,
                ModifiedDate = i.ModifiedDate,
                Solutions = i.Solutions.Select(s => new SolutionResponse
                {
                    Id = s.Id,
                    SolutionName = s.SolutionName,
                    Description = s.Description,
                    Products = s.SolutionProducts.Select(sp => new IssueProductResponse
                    {
                        ProductId = sp.ProductId,
                        ProductName = sp.Product.ProductName,
                        ProductDescription = sp.Product.Description
                    }).ToList()
                }).ToList()
            },
            predicate: i => i.IsDelete == false && (issueCategoryId == null || i.IssueCategoryId == issueCategoryId),
            orderBy: q => isAscending.HasValue
                ? (isAscending.Value ? q.OrderBy(i => i.CreateDate) : q.OrderByDescending(i => i.CreateDate))
                : q.OrderByDescending(i => i.CreateDate),
            size: size,
            page: page,
            include: i => i.Include(i => i.Solutions)
                          .ThenInclude(s => s.SolutionProducts)
                          .ThenInclude(sp => sp.Product)
                          .Include(i => i.IssueCategory)
        );

        int totalItems = issues.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issues retrieved successfully.",
            data = new Paginate<IssueResponse>()
            {
                Page = page,
                Size = size,
                Total = totalItems,
                TotalPages = totalPages,
                Items = issues.Items
            }
        };
    }

    public async Task<ApiResponse> GetIssue(Guid id)
    {
        var issue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: c => c.Id == id && c.IsDelete == false, 
                include: i => i.Include(i => i.Solutions)
                              .ThenInclude(s => s.SolutionProducts)
                              .ThenInclude(sp => sp.Product)
                              .Include(i => i.IssueCategory));

        if (issue == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Issue not found.",
                data = null
            };
        }

        var response = _mapper.Map<IssueResponse>(issue);
        response.Solutions = issue.Solutions?.Select(s => new SolutionResponse
        {
            Id = s.Id,
            SolutionName = s.SolutionName,
            Description = s.Description,
            Products = s.SolutionProducts?.Select(sp => new IssueProductResponse
            {
                ProductId = sp.Product.Id,
                ProductName = sp.Product.ProductName,
                ProductDescription = sp.Product.Description
            }).ToList()
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
        var existingIssue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == false,
                include: i => i.Include(i => i.Solutions));

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

        return new ApiResponse
        {
            status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue updated successfully with solutions and products." : "Failed to update Issue.",
            data = _mapper.Map<IssueResponse>(existingIssue)
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
            status = isSuccessful ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful ? "Issue and related solutions deleted successfully." : "Failed to delete Issue."
        };
    }
}