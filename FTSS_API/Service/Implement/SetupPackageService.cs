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

namespace FTSS_API.Service.Implement
{
    public class SetupPackageService : BaseService<SetupPackageService>, ISetupPackageService
    {
        public SetupPackageService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<SetupPackageService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<ApiResponse> AddSetupPackage(List<Guid> productIds, AddSetupPackageRequest request)
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
                    throw new BadHttpRequestException("You need to log in.");
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
                    Userid = userId
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
                    Products = validProducts.Select(p => new ProductResponse
                    {
                        ProductId = p.Id,
                        ProductName = p.ProductName,
                        Price = p.Price
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
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false);

                if (user == null)
                {
                    throw new BadHttpRequestException("You need to log in.");
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
                        Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                        {
                            ProductId = spd.Product.Id,
                            ProductName = spd.Product.ProductName,
                            Price = spd.Product.Price
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
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (user == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }

                // Lấy danh sách SetupPackage của user có Role là Customer, bao gồm UserName và Id
                var query = _unitOfWork.Context.Set<SetupPackage>()
                    .Include(sp => sp.User) // Bao gồm thông tin User
                    .Include(sp => sp.SetupPackageDetails)
                        .ThenInclude(spd => spd.Product)
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
                            Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                ProductId = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Price = spd.Product.Price
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


        public async Task<ApiResponse> GetListSetupPackageShop(int pageNumber, int pageSize, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (user == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }

                // Lấy danh sách SetupPackage của User có Role là Admin hoặc Manager, bao gồm danh sách sản phẩm
                var query = _unitOfWork.Context.Set<SetupPackage>()
                    .Include(sp => sp.SetupPackageDetails) // Bao gồm danh sách sản phẩm
                    .ThenInclude(spd => spd.Product) // Bao gồm thông tin sản phẩm
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
                    Products = sp.SetupPackageDetails.Select(spd => new ProductResponse
                    {
                        ProductId = spd.Product.Id,
                        ProductName = spd.Product.ProductName,
                        Price = spd.Product.Price
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



        public async Task<ApiResponse> RemoveSetupPackage(Guid id)
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
                    throw new BadHttpRequestException("You need to log in.");
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
    }
}
