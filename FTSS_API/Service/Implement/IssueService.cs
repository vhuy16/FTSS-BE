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
using System.Text.Json;
using FTSS_API.Payload.Request.Solution;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
using LinqKit;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using MRC_API.Utils;
using Supabase.Storage;
using Client = Supabase.Client;

public class IssueService : BaseService<IssueService>, IIssueService
{
    private readonly SupabaseUltils _supabaseImageService;
    private readonly HtmlSanitizerUtils _sanitizer;

    public IssueService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<IssueService> logger, IMapper mapper,
        SupabaseUltils supabaseImageService, HtmlSanitizerUtils sanitizer,
        IHttpContextAccessor httpContextAccessor)
        : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _supabaseImageService = supabaseImageService;
        _sanitizer = sanitizer;
    }


// ...

    public async Task<ApiResponse> CreateIssue(AddUpdateIssueRequest request, Client client)
    {
        string issueImage = null;

        if (request.IssueImage != null)
        {
            var imageUrls =
                await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.IssueImage }, client);
            issueImage = imageUrls.FirstOrDefault();
        }

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IssueCategoryId = request.IssueCategoryId,
            CreateDate = DateTime.UtcNow,
            IsDelete = false,
            IssueImage = issueImage,
        };

        await _unitOfWork.GetRepository<Issue>().InsertAsync(issue);

        // Parse JSON strings from SolutionsJson
        List<SolutionDto> parsedSolutions = new List<SolutionDto>();
        foreach (var solutionJson in request.SolutionsJson)
        {
            try
            {
                // Deserialize each JSON string into a SolutionDto object
                var solution = JsonSerializer.Deserialize<SolutionDto>(solutionJson);
                if (solution != null)
                {
                    parsedSolutions.Add(solution);
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Invalid solution JSON format.",
                    data = ex.Message
                };
            }
        }

        if (parsedSolutions.Any())
        {
            var solutions = new List<Solution>();
            var solutionProducts = new List<SolutionProduct>();

            foreach (var solutionRequest in parsedSolutions)
            {
                var description = _sanitizer.Sanitize(solutionRequest.Description);
                var solution = new Solution
                {
                    Id = Guid.NewGuid(),
                    SolutionName = solutionRequest.SolutionName,
                    Description = description,
                    IssueId = issue.Id,
                    CreateDate = DateTime.UtcNow,
                    IsDelete = false
                };
                solutions.Add(solution);

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

            await _unitOfWork.GetRepository<Solution>().InsertRangeAsync(solutions);
            if (solutionProducts.Any())
            {
                await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(solutionProducts);
            }
        }

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
                        .ThenInclude(p => p.Images)
                );
        }

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Issue created successfully with solutions and related products.",
            data = _mapper.Map<IssueResponse>(createdIssue)
        };
    }

    public async Task<ApiResponse> GetAllIssues(int page, int size, bool? isAscending, Guid? issueCategoryId = null, 
        string issueTitle = null, bool includeDeletedSolutions = false)
    {
        Expression<Func<Issue, bool>> predicate = i => true;

        if (issueCategoryId.HasValue)
        {
            predicate = predicate.And(i => i.IssueCategoryId == issueCategoryId.Value);
        }

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
                    .Include(i => i.IssueCategory)
                    .Include(i => includeDeletedSolutions ? i.Solutions : i.Solutions.Where(s => s.IsDelete == false))
                    .ThenInclude(s => s.SolutionProducts)
                    .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.Images),
                page,
                size);

        var mapped = _mapper.Map<List<IssueResponse>>(issues.Items);

        int totalItems = issues.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);

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
            message = "Lấy danh sách vấn đề thành công.",
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

    public async Task<ApiResponse> UpdateIssue(Guid id, AddUpdateIssueRequest request, Client client)
    {
        // Tìm issue cần cập nhật
        var existingIssue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == false,
                include: source => source
                    .Include(i => i.Solutions)
                    .ThenInclude(s => s.SolutionProducts)
            );

        if (existingIssue == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Issue not found",
                data = null
            };
        }

        // Xử lý hình ảnh nếu có
        string issueImage = existingIssue.IssueImage;
        if (request.IssueImage != null)
        {
            // Giả sử bạn muốn thay thế hình ảnh cũ
            var imageUrls =
                await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.IssueImage }, client);
            issueImage = imageUrls.FirstOrDefault();
        }

        // Cập nhật thông tin cơ bản của issue
        existingIssue.Title = request.Title ?? existingIssue.Title;
        existingIssue.Description = _sanitizer.Sanitize(request.Description) ?? existingIssue.Description;
        existingIssue.IssueCategoryId = request.IssueCategoryId ?? existingIssue.IssueCategoryId;
        existingIssue.IssueImage = issueImage;
        existingIssue.ModifiedDate = DateTime.Now;

        // Xử lý Solutions nếu có
        if (request.SolutionsJson != null && request.SolutionsJson.Any())
        {
            // Xóa các solutions hiện tại
            var existingSolutions = existingIssue.Solutions.ToList();
            if (existingSolutions.Any())
            {
                _unitOfWork.GetRepository<Solution>().DeleteRangeAsync(existingSolutions);
            }

            // Parse và tạo mới solutions
            List<SolutionDto> parsedSolutions = new List<SolutionDto>();
            foreach (var solutionJson in request.SolutionsJson)
            {
                try
                {
                    var solution = JsonSerializer.Deserialize<SolutionDto>(solutionJson);
                    if (solution != null)
                    {
                        parsedSolutions.Add(solution);
                    }
                }
                catch (Exception ex)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid solution JSON format.",
                        data = ex.Message
                    };
                }
            }

            if (parsedSolutions.Any())
            {
                var solutions = new List<Solution>();
                var solutionProducts = new List<SolutionProduct>();

                foreach (var solutionRequest in parsedSolutions)
                {
                    var solution = new Solution
                    {
                        Id = Guid.NewGuid(),
                        SolutionName = solutionRequest.SolutionName,
                        Description = solutionRequest.Description,
                        IssueId = existingIssue.Id,
                        CreateDate = DateTime.UtcNow,
                        IsDelete = false,
                        ModifiedDate = DateTime.UtcNow,
                    };
                    solutions.Add(solution);

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

                await _unitOfWork.GetRepository<Solution>().InsertRangeAsync(solutions);
                if (solutionProducts.Any())
                {
                    await _unitOfWork.GetRepository<SolutionProduct>().InsertRangeAsync(solutionProducts);
                }
            }
        }

        // Cập nhật issue
        _unitOfWork.GetRepository<Issue>().UpdateAsync(existingIssue);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        if (isSuccessful)
        {
            var updatedIssue = await _unitOfWork.GetRepository<Issue>()
                .SingleOrDefaultAsync(
                    predicate: i => i.Id == id,
                    include: source => source
                        .Include(i => i.IssueCategory)
                        .Include(i => i.Solutions)
                        .ThenInclude(s => s.SolutionProducts)
                        .ThenInclude(sp => sp.Product)
                        .ThenInclude(p => p.Images)
                );

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Issue updated successfully",
                data = _mapper.Map<IssueResponse>(updatedIssue)
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status400BadRequest.ToString(),
            message = "Failed to update issue",
            data = null
        };
    }

    public async Task<ApiResponse> DeleteIssue(Guid id)
    {
        // Lấy issue cần xóa cùng với các Solutions liên quan
        var issue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == false,
                include: i => i.Include(i => i.Solutions));

        // Kiểm tra nếu không tìm thấy issue
        if (issue == null)
            return new ApiResponse 
            { 
                status = StatusCodes.Status404NotFound.ToString(), 
                message = "Không tìm thấy vấn đề." 
            };

        // Soft delete issue
        issue.IsDelete = true;
        issue.ModifiedDate = DateTime.UtcNow;
        _unitOfWork.GetRepository<Issue>().UpdateAsync(issue);

        // Soft delete các solutions liên quan
        if (issue.Solutions != null)
        {
            foreach (var solution in issue.Solutions)
            {
                // Chỉ soft delete nếu solution chưa bị xóa (IsDelete không phải true)
                if (!solution.IsDelete.HasValue || !solution.IsDelete.Value)
                {
                    solution.IsDelete = true;
                    solution.ModifiedDate = DateTime.UtcNow;
                    _unitOfWork.GetRepository<Solution>().UpdateAsync(solution);
                }
            }
        }

        // Lưu thay đổi vào cơ sở dữ liệu
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        // Trả về phản hồi
        return new ApiResponse
        {
            status = isSuccessful
                ? StatusCodes.Status200OK.ToString()
                : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful 
                ? "Vấn đề và các giải pháp liên quan đã được xóa thành công." 
                : "Xóa vấn đề thất bại."
        };
    }

    public async Task<ApiResponse> EnableIssue(Guid id)
    {
        // Lấy issue cần kích hoạt cùng với các Solutions liên quan
        var issue = await _unitOfWork.GetRepository<Issue>()
            .SingleOrDefaultAsync(
                predicate: i => i.Id == id && i.IsDelete == true,
                include: i => i.Include(i => i.Solutions));

        if (issue == null)
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Không tìm thấy vấn đề hoặc vấn đề chưa bị xóa."
            };


        // Kích hoạt lại issue
        issue.IsDelete = false;
        issue.ModifiedDate = DateTime.UtcNow;
        _unitOfWork.GetRepository<Issue>().UpdateAsync(issue);

        // Kích hoạt lại các solutions liên quan
        if (issue.Solutions != null)
        {
            foreach (var solution in issue.Solutions)
            {
                // Kiểm tra nếu solution đã bị soft delete (IsDelete là true)
                if (solution.IsDelete.HasValue && solution.IsDelete.Value)
                {
                    solution.IsDelete = false;
                    solution.ModifiedDate = DateTime.UtcNow;
                    _unitOfWork.GetRepository<Solution>().UpdateAsync(solution);
                }
            }
        }

        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isSuccessful
                ? StatusCodes.Status200OK.ToString()
                : StatusCodes.Status500InternalServerError.ToString(),
            message = isSuccessful
                ? "Vấn đề và các giải pháp liên quan đã được kích hoạt lại thành công."
                : "Kích hoạt lại vấn đề thất bại."
        };
    }
}