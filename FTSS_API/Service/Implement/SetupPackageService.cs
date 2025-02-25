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

                // Kiểm tra xem SetupName và Description có bị bỏ trống không
                if (string.IsNullOrWhiteSpace(request.SetupName))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name cannot be empty.",
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
                // Kiểm tra độ dài của SetupName
                if (request.SetupName.Length > 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name must not exceed 10 characters.",
                        data = null
                    };
                }
                // Kiểm tra ký tự đầu tiên có phải chữ hoa không
                if (!char.IsUpper(request.SetupName.Trim()[0]))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name must start with an uppercase letter.",
                        data = null
                    };
                }
                // Kiểm tra xem SetupName đã tồn tại chưa
                bool isSetupNameExists = await _unitOfWork.Context.Set<SetupPackage>()
                    .AnyAsync(sp => sp.SetupName == request.SetupName && sp.IsDelete == false);

                if (isSetupNameExists)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name already exists. Please choose another name.",
                        data = null
                    };
                }

                // Check if image is provided
                if (request.ImageFile == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Image file is required.",
                        data = null
                    };
                }

                string? imageUrl = null;

                // Upload image if provided
                if (request.ImageFile != null)
                {
                    try
                    {
                        // Call your Supabase image upload service
                        imageUrl = (await _supabaseImageService.SendImagesAsync(new List<IFormFile> { request.ImageFile }, client)).FirstOrDefault();

                        if (string.IsNullOrEmpty(imageUrl))
                        {
                            return new ApiResponse
                            {
                                status = StatusCodes.Status500InternalServerError.ToString(),
                                message = "Failed to upload image.",
                                data = null
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = $"An error occurred while uploading the image: {ex.Message}",
                            data = null
                        };
                    }
                }

                // Lấy danh sách sản phẩm hợp lệ theo điều kiện
                var allProducts = await _unitOfWork.Context.Set<Product>()
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.ProductName, p.Price, p.Quantity, p.IsDelete, p.Status, p.SubCategoryId, p.Size })
                    .ToListAsync();

                // Kiểm tra sản phẩm không hợp lệ
                var invalidProducts = allProducts
                    .Where(p => p.Quantity <= 0 || p.IsDelete == true || p.Status != ProductStatusEnum.Available.GetDescriptionFromEnum())
                    .ToList();

                if (invalidProducts.Any())
                {
                    string errorMessage = string.Join(", ", invalidProducts.Select(p => p.ProductName)) +
                                          " has been removed or is out of stock, please add another product.";

                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = errorMessage,
                        data = null
                    };
                }

                // Lọc danh sách sản phẩm hợp lệ
                var validProducts = allProducts
                    .Where(p => p.Quantity > 0 && p.IsDelete == false && p.Status == ProductStatusEnum.Available.GetDescriptionFromEnum())
                    .ToList();

                if (!validProducts.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "No valid products found.",
                        data = null
                    };
                }

                // Lấy danh sách Category của sản phẩm
                var categoryIds = validProducts.Select(p => p.SubCategoryId).Distinct().ToList();
                var categories = await _unitOfWork.Context.Set<SubCategory>()
                    .Where(sc => categoryIds.Contains(sc.Id))
                    .Select(sc => new { sc.Id, sc.Category.CategoryName }) // Lấy tên danh mục từ Category
                    .ToListAsync();

                // Danh sách danh mục bắt buộc
                var requiredCategories = new List<string> { "Bể", "Lọc", "Đèn" };

                // Danh sách danh mục đã có trong danh sách sản phẩm truyền vào
                var existingCategories = categories.Select(c => c.CategoryName).ToList();

                // Kiểm tra danh mục nào bị thiếu
                var missingCategories = requiredCategories.Except(existingCategories).ToList();

                if (missingCategories.Any())
                {
                    string missingMessage = "Missing required products from categories: " + string.Join(", ", missingCategories);

                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = missingMessage,
                        data = null
                    };
                }
                // ❗ Kiểm tra nếu có nhiều hơn 1 sản phẩm thuộc danh mục "Bể"
                var tankProducts = validProducts
                    .Where(p => categories.Any(c => c.Id == p.SubCategoryId && c.CategoryName == "Bể"))
                    .ToList();

                if (tankProducts.Count > 1)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Only one product from category 'Bể' is allowed.",
                        data = null
                    };
                }
                // Lấy kích thước từ sản phẩm thuộc danh mục "Bể"
                string? tankSize = tankProducts.FirstOrDefault()?.Size;
                // Tính tổng giá của các sản phẩm hợp lệ
                decimal totalPrice = validProducts.Sum(p => p.Price);

                // Tạo đối tượng SetupPackage mới
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

                // Thêm SetupPackage vào database
                await _unitOfWork.Context.Set<SetupPackage>().AddAsync(setupPackage);
                await _unitOfWork.CommitAsync();

                // Tạo danh sách SetupPackageDetail từ sản phẩm hợp lệ
                var setupPackageDetails = validProducts.Select(p => new SetupPackageDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id,
                    SetupPackageId = setupPackage.Id,
                    Price = p.Price
                }).ToList();

                // Thêm danh sách vào database
                await _unitOfWork.Context.Set<SetupPackageDetail>().AddRangeAsync(setupPackageDetails);
                await _unitOfWork.CommitAsync();

                // Chuẩn bị dữ liệu trả về
                var response = new SetupPackageResponse
                {
                    SetupPackageId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    Description = setupPackage.Description,
                    TotalPrice = setupPackage.Price,
                    CreateDate = setupPackage.CreateDate,
                    ModifyDate = setupPackage.ModifyDate,
                    Size = tankSize,
                    LinkImage = setupPackage.Image,
                    Products = validProducts.Select(p => new ProductResponse
                    {
                        ProductId = p.Id,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Status = p.Status
                    }).ToList()
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
                    message = "An error occurred while adding setup package.",
                    data = ex.Message
                };
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
                        LinkImage = sp.Image,
                        Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                        {
                            ProductId = spd.Product.Id,
                            ProductName = spd.Product.ProductName,
                            Price = spd.Product.Price,
                            Status = spd.Product.Status,
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
                            LinkImage = sp.Image,
                            Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                ProductId = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Price = spd.Product.Price,
                                Status = spd.Product.Status
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
                    LinkImage = sp.Image,
                    Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        ProductId = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Price = spd.Product.Price,
                        Status = spd.Product.Status,
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
                    LinkImage = setupPackage.Image,
                    Products = setupPackage.SetupPackageDetails.Select(d => new ProductResponse
                    {
                        ProductId = d.Product.Id,
                        ProductName = d.Product.ProductName,
                        Price = d.Product.Price,
                        Status = d.Product.Status,
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
        public async Task<ApiResponse> UpdateSetupPackage(Guid id, AddSetupPackageRequest request, List<Guid> productIds)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false);

                if (user == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Unauthorized: Token is missing or expired.",
                        data = null
                    };
                }

                // Kiểm tra xem SetupName và Description có bị bỏ trống không
                if (string.IsNullOrWhiteSpace(request.SetupName))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name cannot be empty.",
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
                // Kiểm tra độ dài của SetupName
                if (request.SetupName.Length > 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name must not exceed 10 characters.",
                        data = null
                    };
                }
                // Kiểm tra ký tự đầu tiên có phải chữ hoa không
                if (!char.IsUpper(request.SetupName.Trim()[0]))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name must start with an uppercase letter.",
                        data = null
                    };
                }

                var setupPackage = await _unitOfWork.Context.Set<SetupPackage>()
                    .Include(sp => sp.SetupPackageDetails) // Include existing details for removal
                    .FirstOrDefaultAsync(sp => sp.Id == id && sp.IsDelete == false && sp.Userid == userId);

                if (setupPackage == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup package not found or not authorized to update.",
                        data = null
                    };
                }

                // Kiểm tra xem SetupName đã tồn tại chưa (ngoại trừ chính nó)
                bool isSetupNameExists = await _unitOfWork.Context.Set<SetupPackage>()
                    .AnyAsync(sp => sp.SetupName == request.SetupName && sp.Id != id && sp.IsDelete == false);

                if (isSetupNameExists)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Setup name already exists. Please choose another name.",
                        data = null
                    };
                }

                // Lấy danh sách sản phẩm hợp lệ theo điều kiện
                var allProducts = await _unitOfWork.Context.Set<Product>()
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.ProductName, p.Price, p.Quantity, p.IsDelete, p.Status, p.SubCategoryId })
                    .ToListAsync();

                // Kiểm tra sản phẩm không hợp lệ
                var invalidProducts = allProducts
                    .Where(p => p.Quantity <= 0 || p.IsDelete == true || p.Status != ProductStatusEnum.Available.GetDescriptionFromEnum())
                    .ToList();

                if (invalidProducts.Any())
                {
                    string errorMessage = string.Join(", ", invalidProducts.Select(p => p.ProductName)) +
                                          " has been removed or is out of stock, please add another product.";

                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = errorMessage,
                        data = null
                    };
                }

                // Lọc danh sách sản phẩm hợp lệ
                var validProducts = allProducts
                    .Where(p => p.Quantity > 0 && p.IsDelete == false && p.Status == ProductStatusEnum.Available.GetDescriptionFromEnum())
                    .ToList();

                if (!validProducts.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "No valid products found.",
                        data = null
                    };
                }

                // Lấy danh sách Category của sản phẩm
                var categoryIds = validProducts.Select(p => p.SubCategoryId).Distinct().ToList();
                var categories = await _unitOfWork.Context.Set<SubCategory>()
                    .Where(sc => categoryIds.Contains(sc.Id))
                    .Select(sc => new { sc.Id, sc.Category.CategoryName }) // Lấy tên danh mục từ Category
                    .ToListAsync();

                // Danh sách danh mục bắt buộc
                var requiredCategories = new List<string> { "Bể", "Lọc", "Đèn" };

                // Danh sách danh mục đã có trong danh sách sản phẩm truyền vào
                var existingCategories = categories.Select(c => c.CategoryName).ToList();

                // Kiểm tra danh mục nào bị thiếu
                var missingCategories = requiredCategories.Except(existingCategories).ToList();

                if (missingCategories.Any())
                {
                    string missingMessage = "Missing required products from categories: " + string.Join(", ", missingCategories);

                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = missingMessage,
                        data = null
                    };
                }

                // ❗ Kiểm tra nếu có nhiều hơn 1 sản phẩm thuộc danh mục "Bể"
                var tankProducts = validProducts
                    .Where(p => categories.Any(c => c.Id == p.SubCategoryId && c.CategoryName == "Bể"))
                    .ToList();

                if (tankProducts.Count > 1)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Only one product from category 'Bể' is allowed.",
                        data = null
                    };
                }

                // Calculate new total price
                decimal totalPrice = validProducts.Sum(p => p.Price);

                // Update SetupPackage properties
                setupPackage.SetupName = request.SetupName;
                setupPackage.Description = request.Description;
                setupPackage.Price = totalPrice;
                setupPackage.ModifyDate = TimeUtils.GetCurrentSEATime();

                // Remove existing SetupPackageDetails
                _unitOfWork.Context.Set<SetupPackageDetail>().RemoveRange(setupPackage.SetupPackageDetails);

                // Create new SetupPackageDetails based on validProducts
                var setupPackageDetails = validProducts.Select(p => new SetupPackageDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id,
                    SetupPackageId = setupPackage.Id,
                    Price = p.Price
                }).ToList();

                // Add new SetupPackageDetails to the context
                await _unitOfWork.Context.Set<SetupPackageDetail>().AddRangeAsync(setupPackageDetails);

                await _unitOfWork.CommitAsync();

                // Prepare response data
                var response = new SetupPackageResponse
                {
                    SetupPackageId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    Description = setupPackage.Description,
                    TotalPrice = setupPackage.Price,
                    CreateDate = setupPackage.CreateDate,
                    ModifyDate = setupPackage.ModifyDate,
                    Products = validProducts.Select(p => new ProductResponse
                    {
                        ProductId = p.Id,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Status = p.Status
                    }).ToList()
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Setup package updated successfully.",
                    data = response
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
    }

}
