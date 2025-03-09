using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Product;
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
using Newtonsoft.Json;
using Supabase;

namespace FTSS_API.Service.Implement
{
    public class SetupPackageService : BaseService<SetupPackageService>, ISetupPackageService
    {
        private readonly SupabaseUltils _supabaseImageService;

        public SetupPackageService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<SetupPackageService> logger,
            IMapper mapper, IHttpContextAccessor httpContextAccessor, SupabaseUltils supabaseImageService) : base(
            unitOfWork, logger, mapper, httpContextAccessor)
        {
            _supabaseImageService = supabaseImageService;
        }

        public async Task<ApiResponse> GetListSetupPackage(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role == RoleEnum.Customer.GetDescriptionFromEnum());

                if (user == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                // Truy vấn danh sách SetupPackage của userId bằng Generic Repository
                var setupPackagesQuery = await _unitOfWork.GetRepository<SetupPackage>().GetListAsync(
                    predicate: sp => sp.Userid == userId && sp.IsDelete == false,
                    orderBy: sp => isAscending.HasValue && isAscending.Value
                        ? sp.OrderBy(sp => sp.CreateDate)
                        : sp.OrderByDescending(sp => sp.CreateDate),
                    include: sp => sp.Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                        .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
                );

                // Áp dụng phân trang sau khi lấy danh sách
                var pagedSetupPackages = setupPackagesQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Chuyển đổi sang danh sách response
                var setupPackageResponses = pagedSetupPackages.Select(sp => new SetupPackageResponse
                {
                    SetupPackageId = sp.Id,
                    SetupName = sp.SetupName,
                    Description = sp.Description,
                    TotalPrice = sp.Price,
                    CreateDate = sp.CreateDate,
                    ModifyDate = sp.ModifyDate,
                    Size = sp.SetupPackageDetails
                        .Where(spd => spd.Product.SubCategory.Category.CategoryName == "Bể")
                        .Select(spd => spd.Product.Size)
                        .FirstOrDefault(),
                    images = sp.Image,
                    IsDelete = sp.IsDelete,
                    Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        Id = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Quantity = spd.Quantity,
                        Price = spd.Product.Price,
                        InventoryQuantity = spd.Product.Quantity,
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
                    message = "Setup packages retrieved successfully.",
                    data = setupPackageResponses
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
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role == RoleEnum.Manager.GetDescriptionFromEnum());

                if (user == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                // Lấy danh sách SetupPackage của tất cả user có Role là Customer
                var setupPackagesQuery = await _unitOfWork.GetRepository<SetupPackage>().GetListAsync(
                    predicate: sp => sp.User != null &&
                                     sp.User.Role == RoleEnum.Customer.GetDescriptionFromEnum() &&
                                     sp.IsDelete == false,
                    orderBy: sp => isAscending == true
                        ? sp.OrderBy(sp => sp.CreateDate)
                        : sp.OrderByDescending(sp => sp.CreateDate),
                    include: sp => sp.Include(sp => sp.User) // Bao gồm thông tin User
                        .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                        .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
                );

                // Áp dụng phân trang
                var pagedSetupPackages = setupPackagesQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Nhóm danh sách theo UserId và UserName
                var groupedSetupPackages = pagedSetupPackages
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
                                .Where(spd => spd.Product.SubCategory.Category.CategoryName == "Bể")
                                .Select(spd => spd.Product.Size)
                                .FirstOrDefault(),
                            images = sp.Image,
                            IsDelete = sp.IsDelete,
                            Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                Id = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Quantity = spd.Quantity,
                                InventoryQuantity = spd.Product.Quantity,
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
                // Lấy danh sách SetupPackage của User có Role là Manager
                var setupPackagesQuery = await _unitOfWork.GetRepository<SetupPackage>().GetListAsync(
                    predicate: sp => sp.User != null &&
                                     sp.User.Role == RoleEnum.Manager.GetDescriptionFromEnum(),
                    // sp.IsDelete == false,
                    orderBy: sp => isAscending == true
                        ? sp.OrderBy(sp => sp.CreateDate)
                        : sp.OrderByDescending(sp => sp.CreateDate),
                    include: sp => sp.Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                        .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
                );

                // Tổng số bản ghi
                var totalRecords = setupPackagesQuery.Count;

                // Phân trang
                var pagedSetupPackages = setupPackagesQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Chuyển đổi dữ liệu sang SetupPackageResponse
                var responseList = pagedSetupPackages.Select(sp => new SetupPackageResponse
                {
                    SetupPackageId = sp.Id,
                    SetupName = sp.SetupName,
                    Description = sp.Description,
                    TotalPrice = sp.Price,
                    CreateDate = sp.CreateDate,
                    ModifyDate = sp.ModifyDate,
                    Size = sp.SetupPackageDetails
                        .Where(spd => spd.Product.SubCategory?.Category?.CategoryName == "Bể")
                        .Select(spd => spd.Product.Size)
                        .FirstOrDefault(),
                    images = sp.Image,
                    IsDelete = sp.IsDelete,
                    Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        Id = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Quantity = spd.Quantity,
                        InventoryQuantity = spd.Product.Quantity,
                        Price = spd.Product.Price,
                        Status = spd.Product.Status,
                        IsDelete = sp.IsDelete,
                        CategoryName = spd.Product.SubCategory?.Category?.CategoryName,
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
                var setupPackage = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                    selector: sp => new SetupPackageResponse
                    {
                        SetupPackageId = sp.Id,
                        SetupName = sp.SetupName,
                        Description = sp.Description,
                        TotalPrice = sp.Price,
                        CreateDate = sp.CreateDate,
                        ModifyDate = sp.ModifyDate,
                        Size = sp.SetupPackageDetails
                            .Where(spd => spd.Product.SubCategory.Category.CategoryName == "Bể")
                            .Select(spd => spd.Product.Size)
                            .FirstOrDefault(),
                        images = sp.Image,
                        IsDelete = sp.IsDelete,
                        Products = sp.SetupPackageDetails
                            .GroupBy(spd => spd.Product.Id) // Nhóm sản phẩm theo ID
                            .Select(group => new ProductResponse
                            {
                                Id = group.Key,
                                ProductName = group.First().Product.ProductName,
                                Quantity = group.Sum(spd => spd.Quantity), // Cộng dồn số lượng
                                InventoryQuantity = group.First().Product.Quantity,
                                Price = group.First().Product.Price,
                                Status = group.First().Product.Status,
                                IsDelete = group.First().Product.IsDelete,
                                CategoryName = group.First().Product.SubCategory.Category.CategoryName,
                                images = group.First().Product.Images
                                    .Where(img => img.IsDelete == false)
                                    .OrderBy(img => img.CreateDate)
                                    .Select(img => img.LinkImage)
                                    .FirstOrDefault()
                            }).ToList()
                    },
                    predicate: sp => sp.Id == id && sp.IsDelete == false,
                    include: source => source
                        .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                        .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
                );

                // Kiểm tra nếu không tìm thấy
                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup package not found.",
                        data = null
                    };
                }

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Setup package details retrieved successfully.",
                    data = setupPackage
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
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum() ||
                                     u.Role == RoleEnum.Customer.GetDescriptionFromEnum()));

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

        public async Task<ApiResponse> AddSetupPackage(List<ProductSetupItem> productids,
            AddSetupPackageRequest request, Client client)
        {
            try
            {
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum() ||
                                     u.Role == RoleEnum.Customer.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.", data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(request.SetupName) || request.SetupName.Length > 10 ||
                    !char.IsUpper(request.SetupName.Trim()[0]))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "SetupName cannot be empty, must be uppercase, and must be less than 10 characters.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(), message = "Description cannot be empty.",
                        data = null
                    };
                }

                if (productids == null || !productids.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(), message = "Product list cannot be empty.",
                        data = null
                    };
                }

                if (productids.Any(p => p.Quantity == null || p.Quantity <= 0))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Each product must have a positive integer quantity.", data = null
                    };
                }

                var isSetupNameExists = await _unitOfWork.GetRepository<SetupPackage>()
                    .SingleOrDefaultAsync(predicate: p => p.SetupName.Equals(request.SetupName));
                if (isSetupNameExists != null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(), message = "Setup name already exists.",
                        data = null
                    };
                }

                bool isManager = user.Role == RoleEnum.Manager.GetDescriptionFromEnum();
                if (isManager && request.ImageFile == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(), message = "Image is required for Manager.",
                        data = null
                    };
                }

                string? imageUrl = null;
                if (request.ImageFile != null)
                {
                    imageUrl = (await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.ImageFile },
                        client)).FirstOrDefault();
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = "Failed to upload image.", data = null
                        };
                    }
                }

                var productIds = productids.Select(p => p.ProductId).ToList();
                var allProducts = await _unitOfWork.GetRepository<Product>().GetListAsync(
                    predicate: p => productIds.Contains(p.Id),
                    include: p => p.Include(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                        .Include(p => p.Images)
                        .Include(p => p.SetupPackageDetails)
                );
                // Kiểm tra các ProductId có tồn tại trong DB hay không
                var missingProducts = productids.Where(p => !allProducts.Any(prod => prod.Id == p.ProductId)).ToList();
                if (missingProducts.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message =
                            $"Some products do not exist: {string.Join(", ", missingProducts.Select(p => p.ProductId))}.",
                        data = null
                    };
                }


                var insufficientStockProducts = allProducts
                    .Where(p => productids.First(pi => pi.ProductId == p.Id).Quantity > p.Quantity)
                    .Select(p => p.ProductName)
                    .ToList();

                if (insufficientStockProducts.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = $"Insufficient stock for products: {string.Join(", ", insufficientStockProducts)}.",
                        data = null
                    };
                }

                var requiredCategories = new List<string> { "Bể", "Lọc", "Đèn" };
                var foundCategories = allProducts.Select(p => p.SubCategory.Category.CategoryName).Distinct().ToList();
                var missingCategories = requiredCategories.Except(foundCategories).ToList();

                if (missingCategories.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = $"Missing required category: {string.Join(", ", missingCategories)}.",
                        data = null
                    };
                }

                // Lọc danh sách sản phẩm có CategoryName = "Bể"
                var categoryBeProducts = allProducts.Where(p => p.SubCategory.Category.CategoryName == "Bể").ToList();

                // Kiểm tra nếu có nhiều hơn 1 sản phẩm thuộc Category "Bể"
                if (categoryBeProducts.Count > 1)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Only one product from category 'Bể' can be added.",
                        data = null
                    };
                }

                // Kiểm tra nếu sản phẩm thuộc Category "Bể" có số lượng > 1
                foreach (var product in categoryBeProducts)
                {
                    var requestedProduct = productids.FirstOrDefault(p => p.ProductId == product.Id);
                    if (requestedProduct != null && requestedProduct.Quantity > 1)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message =
                                $"Product '{product.ProductName}' in category 'Bể' can only have a quantity of 1.",
                            data = null
                        };
                    }
                }

                var invalidProducts = allProducts.Where(p =>
                    p.Status == ProductStatusEnum.Unavailable.GetDescriptionFromEnum() || p.IsDelete == true).ToList();
                if (invalidProducts.Any())
                {
                    var invalidProductNames = string.Join(", ", invalidProducts.Select(p => p.ProductName));
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = $"Invalid products detected: {invalidProductNames}.", data = null
                    };
                }

                decimal totalPrice = productids.Sum(p =>
                    allProducts.First(prod => prod.Id == p.ProductId).Price * (p.Quantity ?? 1));

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

                await _unitOfWork.GetRepository<SetupPackage>().InsertAsync(setupPackage);
                await _unitOfWork.CommitAsync();

                var setupPackageDetails = productids.Select(p => new SetupPackageDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.ProductId,
                    SetupPackageId = setupPackage.Id,
                    Quantity = p.Quantity ?? 1,
                    Price = allProducts.First(prod => prod.Id == p.ProductId).Price * (p.Quantity ?? 1)
                }).ToList();

                await _unitOfWork.GetRepository<SetupPackageDetail>().InsertRangeAsync(setupPackageDetails);
                await _unitOfWork.CommitAsync();

                setupPackage = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                    predicate: sp => sp.Id == setupPackage.Id,
                    include: sp => sp.Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                );
                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status500InternalServerError.ToString(),
                        message = "Setup package retrieval failed after creation.",
                        data = null
                    };
                }


                // 🔹 **Chuẩn bị response `SetupPackageResponse`**
                var response = new SetupPackageResponse
                {
                    SetupPackageId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    Description = setupPackage.Description,
                    TotalPrice = setupPackage.Price,
                    CreateDate = setupPackage.CreateDate,
                    ModifyDate = setupPackage.ModifyDate,
                    Size = setupPackage.SetupPackageDetails
                        .Where(spd => spd.Product.SubCategory.Category.CategoryName == "Bể")
                        .Select(spd => spd.Product.Size)
                        .FirstOrDefault(),
                    images = setupPackage.Image,
                    IsDelete = setupPackage.IsDelete,
                    Products = setupPackageDetails.Select(d =>
                    {
                        var product = allProducts.FirstOrDefault(p => p.Id == d.ProductId);
                        if (product == null)
                        {
                            return null; // Nếu không tìm thấy sản phẩm, bỏ qua
                        }

                        return new ProductResponse
                        {
                            Id = d.ProductId,
                            ProductName = product.ProductName,
                            Price = product.Price,
                            Quantity = d.Quantity,
                            InventoryQuantity = product.Quantity,
                            Status = product.Status,
                            IsDelete = product.IsDelete,

                            CategoryName = product.SubCategory?.Category?.CategoryName, // Kiểm tra null
                            images = product.Images?.OrderBy(img => img.CreateDate).FirstOrDefault()
                                ?.LinkImage // Kiểm tra null
                        };
                    }).Where(p => p != null).ToList()
                };


                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Setup package added successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while adding setup package.", data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> CopySetupPackage(Guid setupPackageId)
        {
            try
            {
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role == RoleEnum.Customer.GetDescriptionFromEnum());

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.", data = null
                    };
                }

                var existingSetup = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                    predicate: sp => sp.Id == setupPackageId && sp.IsDelete == false,
                    include: sp => sp.Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product));

                if (existingSetup == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(), message = "Setup package not found.",
                        data = null
                    };
                }


                var newSetupPackage = new SetupPackage
                {
                    Id = Guid.NewGuid(),
                    SetupName = $"{existingSetup.SetupName}_Copy",
                    Description = existingSetup.Description,
                    Price = existingSetup.Price,
                    CreateDate = TimeUtils.GetCurrentSEATime(),
                    ModifyDate = TimeUtils.GetCurrentSEATime(),
                    IsDelete = false,
                    Userid = userId, // Chuyển quyền sở hữu cho user hiện tại
                    Image = existingSetup.Image
                };

                await _unitOfWork.GetRepository<SetupPackage>().InsertAsync(newSetupPackage);
                await _unitOfWork.CommitAsync();

                // Sao chép chi tiết SetupPackageDetails
                var newSetupPackageDetails = existingSetup.SetupPackageDetails.Select(spd => new SetupPackageDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = spd.ProductId,
                    SetupPackageId = newSetupPackage.Id,
                    Quantity = spd.Quantity,
                    Price = spd.Price
                }).ToList();

                await _unitOfWork.GetRepository<SetupPackageDetail>().InsertRangeAsync(newSetupPackageDetails);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Setup package copied successfully.",
                    data = new { newSetupPackageId = newSetupPackage.Id }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while copying setup package.", data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> UpdateSetupPackage(
            List<ProductSetupItem>? productIds,
            Guid setupPackageId,
            AddSetupPackageRequest request,
            Client client)
        {
            try
            {
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role == RoleEnum.Customer.GetDescriptionFromEnum());

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                var setupPackage = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                    predicate: sp => sp.Id == setupPackageId && sp.Userid == userId && sp.IsDelete == false,
                    include: sp => sp.Include(sp => sp.SetupPackageDetails));

                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup package not found.",
                        data = null
                    };
                }

                setupPackage.SetupName = request.SetupName ?? setupPackage.SetupName;
                setupPackage.Description = request.Description ?? setupPackage.Description;
                setupPackage.ModifyDate = TimeUtils.GetCurrentSEATime();

                if (request.ImageFile != null)
                {
                    var imageUrl =
                        (await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.ImageFile }, client))
                        .FirstOrDefault();
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        setupPackage.Image = imageUrl;
                    }
                }

                if (!string.IsNullOrEmpty(request.ProductItemsJson))
                {
                    var newProductList =
                        JsonConvert.DeserializeObject<List<ProductSetupItem>>(request.ProductItemsJson);
                    if (newProductList != null && newProductList.Any())
                    {
                        var newProductIds = newProductList.Select(p => p.ProductId).ToList();
                        var existingSetupPackageDetails = await _unitOfWork.GetRepository<SetupPackageDetail>()
                            .GetListAsync(predicate: spd => spd.SetupPackageId == setupPackage.Id);

                        var existingProductIds = existingSetupPackageDetails.Select(spd => spd.ProductId).ToList();

                        var newProducts = await _unitOfWork.GetRepository<Product>().GetListAsync(
                            predicate: p => newProductIds.Contains(p.Id));

                        // Nhóm sản phẩm theo ProductId và tính tổng Quantity
                        var groupedProducts = newProductList
                            .GroupBy(p => p.ProductId)
                            .Select(g => new
                            {
                                ProductId = g.Key,
                                TotalQuantity = g.Sum(p => p.Quantity ?? 1) // Nếu Quantity null thì mặc định là 1
                            })
                            .ToList();

                        var setupPackageDetailsToDelete = existingSetupPackageDetails
                            .Where(spd => !newProductIds.Contains(spd.ProductId))
                            .ToList();

                        if (setupPackageDetailsToDelete.Any())
                        {
                            _unitOfWork.GetRepository<SetupPackageDetail>()
                                .DeleteRangeAsync(setupPackageDetailsToDelete);
                        }

                        var newSetupPackageDetails = new List<SetupPackageDetail>();

                        foreach (var gp in groupedProducts)
                        {
                            var existingDetail =
                                existingSetupPackageDetails.FirstOrDefault(spd => spd.ProductId == gp.ProductId);
                            var product = newProducts.FirstOrDefault(prod => prod.Id == gp.ProductId);

                            if (product != null)
                            {
                                if (existingDetail != null)
                                {
                                    // Nếu sản phẩm đã có, cập nhật số lượng
                                    existingDetail.Quantity += gp.TotalQuantity;
                                    existingDetail.Price = product.Price * existingDetail.Quantity;
                                }
                                else
                                {
                                    // Nếu sản phẩm chưa có, thêm mới
                                    newSetupPackageDetails.Add(new SetupPackageDetail
                                    {
                                        Id = Guid.NewGuid(),
                                        ProductId = gp.ProductId,
                                        SetupPackageId = setupPackage.Id,
                                        Quantity = gp.TotalQuantity,
                                        Price = product.Price * gp.TotalQuantity
                                    });
                                }
                            }
                        }

                        if (newSetupPackageDetails.Any())
                        {
                            await _unitOfWork.GetRepository<SetupPackageDetail>()
                                .InsertRangeAsync(newSetupPackageDetails);
                        }
                    }
                }

                _unitOfWork.GetRepository<SetupPackage>().UpdateAsync(setupPackage);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Setup package updated successfully.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while updating setup package.",
                    data = ex.Message
                };
            }
        }

        public async Task<bool> enableSetupPackage(Guid setupPackageId)
        {
            var setupPackage = await _unitOfWork.GetRepository<SetupPackage>()
                .SingleOrDefaultAsync(predicate: sp => sp.Id == setupPackageId);
            if (setupPackage.IsDelete == true)
            {
                setupPackage.IsDelete = false;

                _unitOfWork.GetRepository<SetupPackage>().UpdateAsync(setupPackage);
                await _unitOfWork.CommitAsync();
                return true;
            }
            else
            {
                return false;
            }
        }
        // public Task<ApiResponse> UpdateSetupPackage(Guid setupPackageId, List<ProductSetupItem> productids,
        //     AddSetupPackageRequest request, Client client)
        // {
        //     throw new NotImplementedException();
        // }
    }
}