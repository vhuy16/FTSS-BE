using System.Linq.Expressions;
using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.Issue;
using FTSS_API.Payload.Response.SetupPackage;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
using LinqKit;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Supabase.Storage;
using Client = Supabase.Client;

public class IssueService : BaseService<IssueService>, IIssueService
{ 
    private readonly SupabaseUltils _supabaseImageService;
    public IssueService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<IssueService> logger, IMapper mapper, SupabaseUltils supabaseImageService,
        IHttpContextAccessor httpContextAccessor)
        : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _supabaseImageService = supabaseImageService;
    }

    public async Task<ApiResponse> CreateIssue(AddUpdateIssueRequest request, Client client)
    {string issueImage = null;

        if (request.IssueImage != null)
        {
            var imageUrls = await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.IssueImage }, client);
            issueImage = imageUrls.FirstOrDefault(); // Vì chỉ có 1 ảnh
        }
        // Create the Issue without including Solutions in the mapping
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IssueCategoryId = request.IssueCategoryId,
            CreateDate = DateTime.UtcNow,
            IsDelete = false,
            IssueImage = issueImage,
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
        
        int totalItems = issues.Total;  // Total number of items without pagination
        int totalPages = (int)Math.Ceiling((double)totalItems / size);  // Calculate total pages

        // Paginate result
        var paginatedResult = new Paginate<IssueResponse>
        {
            Page = page,
            Size = size,
            Total = totalItems,
            TotalPages = totalPages,
            Items = mapped
        };

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issues retrieved successfully.",
            data = paginatedResult
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
    try
    {
        // Get existing issue with all related data needed for the update
        var existingIssue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == false,
                include: i => i
                    .Include(i => i.IssueCategory)
                    .Include(i => i.Solutions.Where(s => s.IsDelete == false))
                    .ThenInclude(s => s.SolutionProducts)
                    .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.Images)
            );

        if (existingIssue == null)
        {
            return new ApiResponse 
            { 
                status = StatusCodes.Status404NotFound.ToString(), 
                message = "Issue not found." 
            };
        }

        // Update issue properties
        existingIssue.Title = request.Title;
        existingIssue.Description = request.Description;
        existingIssue.IssueCategoryId = request.IssueCategoryId;
        existingIssue.ModifiedDate = DateTime.UtcNow;

         _unitOfWork.GetRepository<Issue>().UpdateAsync(existingIssue);

        // Handle solutions update
        if (request.Solutions != null)
        {
            var existingSolutions = existingIssue.Solutions?.ToList() ?? new List<Solution>();
            var solutionIdsToKeep = request.Solutions
                .Where(s => s.Id.HasValue)
                .Select(s => s.Id.Value)
                .ToList();

            // Process existing solutions to update or soft delete
            foreach (var existingSolution in existingSolutions)
            {
                var solutionRequest = request.Solutions
                    .FirstOrDefault(s => s.Id.HasValue && s.Id.Value == existingSolution.Id);

                if (solutionRequest != null)
                {
                    // Update existing solution
                    existingSolution.SolutionName = solutionRequest.SolutionName;
                    existingSolution.Description = solutionRequest.Description;
                    existingSolution.ModifiedDate = DateTime.UtcNow;
                    
                     _unitOfWork.GetRepository<Solution>().UpdateAsync(existingSolution);

                    // Update solution products
                    await UpdateSolutionProducts(existingSolution.Id, solutionRequest.ProductIds);
                }
                else
                {
                    // Soft delete solution not included in request
                    existingSolution.IsDelete = true;
                    existingSolution.ModifiedDate = DateTime.UtcNow;
                    
                     _unitOfWork.GetRepository<Solution>().UpdateAsync(existingSolution);
                }
            }

            // Add new solutions
            var newSolutions = request.Solutions
                .Where(s => !s.Id.HasValue || !existingSolutions.Any(es => es.Id == s.Id.Value))
                .ToList();

            foreach (var newSolutionRequest in newSolutions)
            {
                var newSolutionId = Guid.NewGuid();
                var newSolution = new Solution
                {
                    Id = newSolutionId,
                    SolutionName = newSolutionRequest.SolutionName,
                    Description = newSolutionRequest.Description,
                    IssueId = existingIssue.Id,
                    CreateDate = DateTime.UtcNow,
                    IsDelete = false
                };

                await _unitOfWork.GetRepository<Solution>().InsertAsync(newSolution);

                // Add solution products
                if (newSolutionRequest.ProductIds != null && newSolutionRequest.ProductIds.Any())
                {
                    await AddSolutionProducts(newSolutionId, newSolutionRequest.ProductIds);
                }
            }
        }

        // Commit all changes at once
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        if (!isSuccessful)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Failed to update Issue."
            };
        }

        // Get the updated issue with all related data for response
        var updatedIssue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id,
                include: source => source
                    .Include(i => i.IssueCategory)
                    .Include(i => i.Solutions.Where(s => s.IsDelete == false))
                    .ThenInclude(s => s.SolutionProducts)
                    .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.Images)
            );

        // Map to response using AutoMapper
        var issueResponse = _mapper.Map<IssueResponse>(updatedIssue);

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issue updated successfully with solutions and products.",
            data = issueResponse
        };
    }
    catch (Exception ex)
    {
        // Log the exception
        _logger.LogError(ex, "Error updating issue with ID {IssueId}", id);
        
        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(),
            message = "An unexpected error occurred while updating the issue.",
            data = ex.Message
        };
    }
}

private async Task AddSolutionProducts(Guid solutionId, List<Guid> productIds)
{
    var solutionProducts = productIds
        .Select(productId => new SolutionProduct
        {
            Id = Guid.NewGuid(),
            SolutionId = solutionId,
            ProductId = productId,
            CreateDate = DateTime.UtcNow
        })
        .ToList();

    await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(solutionProducts);
}

private async Task UpdateSolutionProducts(Guid solutionId, List<Guid>? newProductIds)
{
    var repository = _unitOfWork.GetRepository<SolutionProduct>();
    
    // Always remove all existing and add new ones (cleaner approach)
    var existingProducts = await repository.GetListAsync(predicate: sp => sp.SolutionId == solutionId);
    if (existingProducts != null)
    {
       repository.DeleteRangeAsync(existingProducts);
    }

    if (newProductIds != null && newProductIds.Any())
    {
        await AddSolutionProducts(solutionId, newProductIds);
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