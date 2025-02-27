using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SetupPackage;
using FTSS_API.Payload.Response.SetupPackage;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supabase;

namespace FTSS_API.Service.Implement
{
    public class SetupPackageService : BaseService<SetupPackageService>, ISetupPackageService
    {
        private readonly SupabaseUltils _supabaseImageService;
        public SetupPackageService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<SetupPackageService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, SupabaseUltils supabaseImageService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _supabaseImageService = supabaseImageService;
        }

        public async Task<ApiResponse> AddSetupPackage(List<Guid> productIds, AddSetupPackageRequest request, Supabase.Client client)
        {
            try
            {
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() ||
                                     u.Role == RoleEnum.Manager.GetDescriptionFromEnum() ||
                                     u.Role == RoleEnum.Customer.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(request.SetupName))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name cannot be empty.",
                        data = null
                    };
                }

                if (request.SetupName.Length > 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name must not exceed 10 characters.",
                        data = null
                    };
                }

                if (!char.IsUpper(request.SetupName.Trim()[0]))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name must start with an uppercase letter.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Description cannot be empty.", data = null };
                }

                bool isSetupNameExists = await _unitOfWork.Context.Set<SetupPackage>()
                    .AnyAsync(sp => sp.SetupName == request.SetupName && sp.IsDelete == false);

                if (isSetupNameExists)
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Setup name already exists.", data = null };
                }

                bool isAdminOrManager = user.Role == RoleEnum.Admin.GetDescriptionFromEnum() || user.Role == RoleEnum.Manager.GetDescriptionFromEnum();
                if (isAdminOrManager && request.ImageFile == null)
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Image is required for Admin and Manager.", data = null };
                }

                string? imageUrl = null;
                if (request.ImageFile != null)
                {
                    imageUrl = (await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.ImageFile }, client)).FirstOrDefault();
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return new ApiResponse { status = StatusCodes.Status500InternalServerError.ToString(), message = "Failed to upload image.", data = null };
                    }
                }

                var productCount = productIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                var allProducts = await _unitOfWork.Context.Set<Product>()
                        .Where(p => productCount.Keys.Contains(p.Id))
                        .Include(p => p.SubCategory) // Load bảng SubCategory
                        .ThenInclude(sc => sc.Category) // Load bảng Category từ SubCategory
                        .Include(p => p.Images) // Load danh sách Image
                        .ToListAsync();

                if (!allProducts.Any())
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "No valid products found.", data = null };
                }

                // ✅ Kiểm tra xem có product nào không hợp lệ không (Status = Unavailable hoặc IsDelete = true)
                var invalidProducts = allProducts.Where(p =>
                    p.Status == ProductStatusEnum.Unavailable.GetDescriptionFromEnum() || p.IsDelete == true).ToList();

                if (invalidProducts.Any())
                {
                    var invalidProductNames = string.Join(", ", invalidProducts.Select(p => p.ProductName));
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = $"Invalid products detected: {invalidProductNames}. Please remove them before proceeding.",
                        data = null
                    };
                }

                var categoryIds = allProducts.Select(p => p.SubCategoryId).Distinct().ToList();
                var categories = await _unitOfWork.Context.Set<SubCategory>()
                    .Where(sc => categoryIds.Contains(sc.Id))
                    .Select(sc => new { sc.Id, sc.Category.CategoryName })
                    .ToListAsync();

                var requiredCategories = new List<string> { "Bể", "Lọc", "Đèn" };
                var existingCategories = categories.Select(c => c.CategoryName).ToList();
                var missingCategories = requiredCategories.Except(existingCategories).ToList();

                if (missingCategories.Any())
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Missing required product categories: " + string.Join(", ", missingCategories), data = null };
                }

                var tankProducts = allProducts.Where(p => categories.Any(c => c.Id == p.SubCategoryId && c.CategoryName == "Bể")).ToList();
                int totalTankCount = tankProducts.Sum(p => productCount[p.Id]);

                if (totalTankCount > 1)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Only one product from category 'Bể' is allowed.",
                        data = null
                    };
                }

                string? tankSize = tankProducts.FirstOrDefault()?.Size;
                decimal totalPrice = allProducts.Sum(p => p.Price * productCount[p.Id]);

                var setupPackage = new SetupPackage
                {
                    Id = Guid.NewGuid(),
                    SetupName = request.SetupName,
                    Description = request.Description,
                    Price = totalPrice,
                    CreateDate = TimeUtils.GetCurrentSEATime(),
                    ModifyDate = TimeUtils.GetCurrentSEATime(),
                    IsDelete = false,
                    Userid = userId,
                    Image = imageUrl
                };

                await _unitOfWork.Context.Set<SetupPackage>().AddAsync(setupPackage);
                await _unitOfWork.CommitAsync();

                var setupPackageDetails = allProducts.Select(p => new SetupPackageDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id,
                    SetupPackageId = setupPackage.Id,
                    Quantity = productCount[p.Id],
                    Price = p.Price * productCount[p.Id]
                }).ToList();

                await _unitOfWork.Context.Set<SetupPackageDetail>().AddRangeAsync(setupPackageDetails);
                await _unitOfWork.CommitAsync();

                var response = new SetupPackageResponse
                {
                    SetupPackageId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    Description = setupPackage.Description,
                    TotalPrice = setupPackage.Price,
                    CreateDate = setupPackage.CreateDate,
                    ModifyDate = setupPackage.ModifyDate,
                    Size = tankSize,
                    images = setupPackage.Image,
                    Products = setupPackage.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        ProductId = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Quantity = spd.Quantity,  // Lấy Quantity từ SetupPackageDetail
                        Price = spd.Product.Price,
                        Status = spd.Product.Status,
                        IsDelete = setupPackage.IsDelete,
                        CategoryName = spd.Product.SubCategory.Category.CategoryName,
                        images = spd.Product.Images
                                .Where(img => img.IsDelete == false)
                                .OrderBy(img => img.CreateDate)
                                .Select(img => img.LinkImage)
                                .FirstOrDefault()
                    }).ToList()
                };


                return new ApiResponse { status = StatusCodes.Status201Created.ToString(), message = "Setup package added successfully.", data = response };
            }
            catch (Exception ex)
            {
                return new ApiResponse { status = StatusCodes.Status500InternalServerError.ToString(), message = "An error occurred while adding setup package.", data = ex.Message };
            }
        }


        public async Task<ApiResponse> GetListSetupPackage(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Customer.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                // Lấy danh sách SetupPackage của userId
                var query = _unitOfWork.Context.Set<SetupPackage>()
                    .Where(sp => sp.Userid == userId && sp.IsDelete == false);

                // Sắp xếp dữ liệu theo CreateDate
                query = isAscending.HasValue && isAscending.Value
                    ? query.OrderBy(sp => sp.CreateDate)
                    : query.OrderByDescending(sp => sp.CreateDate);

                // Phân trang
                var setupPackages = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(sp => new SetupPackageResponse
                    {
                        SetupPackageId = sp.Id,
                        SetupName = sp.SetupName,
                        Description = sp.Description,
                        TotalPrice = sp.Price,
                        CreateDate = sp.CreateDate,
                        ModifyDate = sp.ModifyDate,
                        Size = sp.SetupPackageDetails
                        .Where(spd => spd.Product.SubCategory.Category.CategoryName == "Bể")  // Lọc sản phẩm có Category là "Bể"
                        .Select(spd => spd.Product.Size)                 // Lấy Size của sản phẩm
                        .FirstOrDefault(),
                        images = sp.Image,
                        Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                        {
                            ProductId = spd.Product.Id,
                            ProductName = spd.Product.ProductName,
                            Quantity = spd.Quantity,  // Lấy Quantity từ SetupPackageDetail
                            Price = spd.Product.Price,
                            Status = spd.Product.Status,
                            IsDelete = sp.IsDelete,
                            CategoryName = spd.Product.SubCategory.Category.CategoryName,
                            images = spd.Product.Images
                        .Where(img => img.IsDelete == false)
                        .OrderBy(img => img.CreateDate)
                        .Select(img => img.LinkImage)
                        .FirstOrDefault()
                        }).ToList()
                    })
                    .ToListAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Setup packages retrieved successfully.",
                    data = setupPackages
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving setup packages.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListSetupPackageAllUser(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                // Lấy danh sách SetupPackage của user có Role là Customer, bao gồm thông tin User
                var query = _unitOfWork.Context.Set<SetupPackage>()
                    .Include(sp => sp.User) // Bao gồm thông tin User
                    .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                            .ThenInclude(p => p.SubCategory)
                                .ThenInclude(sc => sc.Category) // Truy vấn đến Category
                                .Include(sp => sp.SetupPackageDetails)
                                    .ThenInclude(spd => spd.Product)
                                    .ThenInclude(p => p.Images) // Bao gồm 
                    .Where(sp => sp.User != null &&
                                 sp.User.Role == RoleEnum.Customer.GetDescriptionFromEnum() &&
                                 sp.IsDelete == false);

                // Sắp xếp theo CreateDate
                query = isAscending == true
                    ? query.OrderBy(sp => sp.CreateDate)
                    : query.OrderByDescending(sp => sp.CreateDate);

                // Phân trang
                var setupPackages = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Nhóm danh sách theo UserId và UserName
                var groupedSetupPackages = setupPackages
                    .GroupBy(sp => new { sp.User.Id, sp.User.UserName })
                    .Select(group => new UserSetupPackageResponse
                    {
                        UserId = group.Key.Id,
                        UserName = group.Key.UserName,
                        SetupPackages = group.Select(sp => new SetupPackageResponse
                        {
                            SetupPackageId = sp.Id,
                            SetupName = sp.SetupName,
                            Description = sp.Description,
                            TotalPrice = sp.Price,
                            CreateDate = sp.CreateDate,
                            ModifyDate = sp.ModifyDate,
                            Size = sp.SetupPackageDetails
                                .Select(spd => spd.Product)
                                .FirstOrDefault(p => p.SubCategory != null && p.SubCategory.Category != null && p.SubCategory.Category.CategoryName == "Bể")?.Size, // Lấy Size từ sản phẩm có Category "Bể"
                            images = sp.Image,
                            Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                ProductId = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Quantity = spd.Quantity,  // Lấy Quantity từ SetupPackageDetail
                                Price = spd.Product.Price,
                                Status = spd.Product.Status,
                                IsDelete = sp.IsDelete,
                                CategoryName = spd.Product.SubCategory.Category.CategoryName,
                                images = spd.Product.Images
                            .Where(img => img.IsDelete == false)
                            .OrderBy(img => img.CreateDate)
                            .Select(img => img.LinkImage)
                            .FirstOrDefault()
                            }).ToList()
                        }).ToList()
                    }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of setup packages retrieved successfully.",
                    data = groupedSetupPackages
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving setup packages.",
                    data = ex.Message
                };
            }
        }



        public async Task<ApiResponse> GetListSetupPackageAllShop(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                // Lấy danh sách SetupPackage của User có Role là Admin hoặc Manager, bao gồm danh sách sản phẩm
                var query = _unitOfWork.Context.Set<SetupPackage>()
                                .Include(sp => sp.SetupPackageDetails)
                                .ThenInclude(spd => spd.Product)
                                .ThenInclude(p => p.SubCategory)
                                .ThenInclude(sc => sc.Category)  // Đảm bảo có Category
                                .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
                                    .Where(sp => sp.User != null &&
                                         (sp.User.Role == RoleEnum.Admin.GetDescriptionFromEnum() ||
                                          sp.User.Role == RoleEnum.Manager.GetDescriptionFromEnum()) &&
                                         sp.IsDelete == false);


                // Sắp xếp kết quả theo CreateDate
                query = isAscending == true
                    ? query.OrderBy(sp => sp.CreateDate)
                    : query.OrderByDescending(sp => sp.CreateDate);

                // Thực hiện phân trang
                var totalRecords = await query.CountAsync();
                var setupPackages = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Chuyển đổi dữ liệu sang SetupPackageResponse
                var responseList = setupPackages.Select(sp => new SetupPackageResponse
                {
                    SetupPackageId = sp.Id,
                    SetupName = sp.SetupName,
                    Description = sp.Description,
                    TotalPrice = sp.Price,
                    CreateDate = sp.CreateDate,
                    ModifyDate = sp.ModifyDate,
                    Size = sp.SetupPackageDetails
                        .Where(spd => spd.Product != null &&
                                      spd.Product.SubCategory != null &&
                                      spd.Product.SubCategory.Category != null &&
                                      spd.Product.SubCategory.Category.CategoryName == "Bể")  // Kiểm tra null trước khi truy cập CategoryName
                        .Select(spd => spd.Product.Size)
                        .FirstOrDefault(),
                    images = sp.Image,
                    Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        ProductId = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Quantity = spd.Quantity,  // Lấy Quantity từ SetupPackageDetail
                        Price = spd.Product.Price,
                        Status = spd.Product.Status,
                        IsDelete = sp.IsDelete,
                        CategoryName = spd.Product.SubCategory.Category.CategoryName,
                        images = spd.Product.Images
                    .Where(img => img.IsDelete == false)
                    .OrderBy(img => img.CreateDate)
                    .Select(img => img.LinkImage)
                    .FirstOrDefault()
                    }).ToList()
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Successfully retrieved setup packages.",
                    data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        SetupPackages = responseList
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving setup packages.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetSetUpById(Guid id)
        {
            try
            {
                // Lấy thông tin setup package theo ID
                var setupPackage = await _unitOfWork.GetRepository<SetupPackage>()
                    .SingleOrDefaultAsync(
                        predicate: sp => sp.Id == id && sp.IsDelete == false,
                        include: source => source
                            .Include(sp => sp.SetupPackageDetails)
                            .ThenInclude(d => d.Product)
                            .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category) // Bảo đảm truy vấn đến Category
                            .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
                    );

                // Kiểm tra nếu không tìm thấy gói setup
                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup package not found.",
                        data = null
                    };
                }

                // Lấy kích thước từ sản phẩm có CategoryName là "Bể"
                var productWithCategoryBe = setupPackage.SetupPackageDetails
                    .Select(d => d.Product)
                    .FirstOrDefault(p => p.SubCategory != null && p.SubCategory.Category != null && p.SubCategory.Category.CategoryName == "Bể");

                string? size = productWithCategoryBe?.Size; // Nếu không tìm thấy, size sẽ là null

                // Chuyển dữ liệu sang response
                var response = new SetupPackageResponse
                {
                    SetupPackageId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    Description = setupPackage.Description,
                    TotalPrice = setupPackage.Price,
                    CreateDate = setupPackage.CreateDate,
                    ModifyDate = setupPackage.ModifyDate,
                    Size = size, // Lấy kích thước từ sản phẩm thuộc Category "Bể"
                    images = setupPackage.Image,
                    Products = setupPackage.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        ProductId = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Quantity = spd.Quantity,  // Lấy Quantity từ SetupPackageDetail
                        Price = spd.Product.Price,
                        Status = spd.Product.Status,
                        IsDelete = setupPackage.IsDelete,
                        CategoryName = spd.Product.SubCategory.Category.CategoryName,
                        images = spd.Product.Images
                                .Where(img => img.IsDelete == false)
                                .OrderBy(img => img.CreateDate)
                                .Select(img => img.LinkImage)
                                .FirstOrDefault()
                    }).ToList()
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Setup package details retrieved successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving setup package details.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> RemoveSetupPackage(Guid id)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum() || u.Role == RoleEnum.Customer.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }
                var setupPackage = await _unitOfWork.Context.Set<SetupPackage>()
                    .FirstOrDefaultAsync(sp => sp.Id == id && sp.IsDelete == false);

                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup package not found or already deleted.",
                        data = null
                    };
                }

                setupPackage.IsDelete = true;
                setupPackage.ModifyDate = TimeUtils.GetCurrentSEATime();
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Setup package removed successfully.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while removing the setup package.",
                    data = ex.Message
                };
            }
        }
        public async Task<ApiResponse> UpdateSetupPackage(Guid setupPackageId, List<Guid>? productIds, AddSetupPackageRequest request, Supabase.Client client)
        {
            try
            {
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id == userId &&
                                    u.Status == UserStatusEnum.Available.GetDescriptionFromEnum() &&
                                    u.IsDelete == false);

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                var setupPackage = await _unitOfWork.Context.Set<SetupPackage>()
                    .Include(sp => sp.User) // Load thông tin User đi kèm SetupPackage
                    .SingleOrDefaultAsync(sp => sp.Id == setupPackageId && (sp.IsDelete != true));

                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup package not found.",
                        data = null
                    };
                }

                var setupOwner = setupPackage.User;
                if (setupOwner == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup package has no associated user.",
                        data = null
                    };
                }

                // Role-based access control
                bool isAdminOrManager = user.Role == RoleEnum.Admin.GetDescriptionFromEnum() || user.Role == RoleEnum.Manager.GetDescriptionFromEnum();
                bool isCustomer = user.Role == RoleEnum.Customer.GetDescriptionFromEnum();
                bool ownerIsAdminOrManager = setupOwner.Role == RoleEnum.Admin.GetDescriptionFromEnum() || setupOwner.Role == RoleEnum.Manager.GetDescriptionFromEnum();

                if (isAdminOrManager && !ownerIsAdminOrManager)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "You can only update setups of Admin or Manager accounts.",
                        data = null
                    };
                }

                if (isCustomer && setupPackage.Userid != userId)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "Customers can only update their own setups.",
                        data = null
                    };
                }

                // Validate Setup Name
                if (string.IsNullOrWhiteSpace(request.SetupName))
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Setup name cannot be empty.", data = null };
                }

                if (request.SetupName.Length > 10)
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Setup name must not exceed 10 characters.", data = null };
                }

                if (!char.IsUpper(request.SetupName.Trim()[0]))
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Setup name must start with an uppercase letter.", data = null };
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Description cannot be empty.", data = null };
                }

                bool isSetupNameExists = await _unitOfWork.Context.Set<SetupPackage>()
                        .AnyAsync(sp => sp.SetupName == request.SetupName && sp.Id != setupPackageId && sp.IsDelete != true);

                if (isSetupNameExists)
                {
                    return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Setup name already exists.", data = null };
                }

                // Handle Image Upload
                string? imageUrl = setupPackage.Image;
                if (request.ImageFile != null)
                {
                    if (isAdminOrManager)
                    {
                        imageUrl = (await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.ImageFile }, client)).FirstOrDefault();
                        if (string.IsNullOrEmpty(imageUrl))
                        {
                            return new ApiResponse { status = StatusCodes.Status500InternalServerError.ToString(), message = "Failed to upload image.", data = null };
                        }
                    }
                    else
                    {
                        return new ApiResponse { status = StatusCodes.Status403Forbidden.ToString(), message = "Only Admin and Manager can update images.", data = null };
                    }
                }

                if (productIds != null && productIds.Any())
                {
                    var productCount = productIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                    var allProducts = await _unitOfWork.Context.Set<Product>()
                        .Where(p => productCount.Keys.Contains(p.Id))
                        .Include(p => p.SubCategory) // Load bảng SubCategory
                        .ThenInclude(sc => sc.Category) // Load bảng Category từ SubCategory
                        .Include(p => p.Images) // Load danh sách Image
                        .ToListAsync();

                    if (!allProducts.Any())
                    {
                        return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "No valid products found.", data = null };
                    }

                    var categoryIds = allProducts.Select(p => p.SubCategoryId).Distinct().ToList();
                    var categories = await _unitOfWork.Context.Set<SubCategory>()
                        .Where(sc => categoryIds.Contains(sc.Id))
                        .Select(sc => new { sc.Id, sc.Category.CategoryName })
                        .ToListAsync();

                    var requiredCategories = new List<string> { "Bể", "Lọc", "Đèn" };
                    var existingCategories = categories.Select(c => c.CategoryName).ToList();
                    var missingCategories = requiredCategories.Except(existingCategories).ToList();

                    if (missingCategories.Any())
                    {
                        return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Missing required product categories: " + string.Join(", ", missingCategories), data = null };
                    }

                    var tankProducts = allProducts.Where(p => categories.Any(c => c.Id == p.SubCategoryId && c.CategoryName == "Bể")).ToList();
                    int totalTankCount = tankProducts.Sum(p => productCount[p.Id]);

                    if (totalTankCount > 1)
                    {
                        return new ApiResponse { status = StatusCodes.Status400BadRequest.ToString(), message = "Only one product from category 'Bể' is allowed.", data = null };
                    }

                    decimal totalPrice = allProducts.Sum(p => p.Price * productCount[p.Id]);

                    using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();

                    setupPackage.SetupName = request.SetupName;
                    setupPackage.Description = request.Description;
                    setupPackage.Image = imageUrl;
                    setupPackage.Price = totalPrice;
                    setupPackage.ModifyDate = TimeUtils.GetCurrentSEATime();

                    _unitOfWork.Context.Set<SetupPackageDetail>().RemoveRange(
                        _unitOfWork.Context.Set<SetupPackageDetail>().Where(spd => spd.SetupPackageId == setupPackage.Id));

                    var setupPackageDetails = allProducts.Select(p => new SetupPackageDetail
                    {
                        Id = Guid.NewGuid(),
                        ProductId = p.Id,
                        SetupPackageId = setupPackage.Id,
                        Quantity = productCount[p.Id],
                        Price = p.Price * productCount[p.Id]
                    }).ToList();

                    await _unitOfWork.Context.Set<SetupPackageDetail>().AddRangeAsync(setupPackageDetails);
                    await _unitOfWork.CommitAsync();
                    await transaction.CommitAsync();
                }

                var response = new SetupPackageResponse
                {
                    SetupPackageId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    Description = setupPackage.Description,
                    TotalPrice = setupPackage.Price,
                    CreateDate = setupPackage.CreateDate,
                    ModifyDate = setupPackage.ModifyDate,
                    Size = setupPackage.SetupPackageDetails
                                .Select(spd => spd.Product)
                                .FirstOrDefault(p => p.SubCategory != null && p.SubCategory.Category != null && p.SubCategory.Category.CategoryName == "Bể")?.Size,
                    images = setupPackage.Image,
                    Products = setupPackage.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        ProductId = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Quantity = spd.Quantity,  // Lấy Quantity từ SetupPackageDetail
                        Price = spd.Product.Price,
                        Status = spd.Product.Status,
                        IsDelete = setupPackage.IsDelete,
                        CategoryName = spd.Product.SubCategory.Category.CategoryName,
                        images = spd.Product.Images
                                .Where(img => img.IsDelete == false)
                                .OrderBy(img => img.CreateDate)
                                .Select(img => img.LinkImage)
                                .FirstOrDefault()
                    }).ToList()
                };

                return new ApiResponse 
                { status = StatusCodes.Status200OK.ToString(),
                    message = "Setup package updated successfully.", 
                    data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateSetupPackage.");
                return new ApiResponse { status = StatusCodes.Status500InternalServerError.ToString(), message = "An error occurred while updating the setup package.", data = null };
            }
        }

    }
}
