﻿using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Product;
using FTSS_API.Payload.Response;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Model.Paginate;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using MRC_API.Utils;

namespace FTSS_API.Service.Implement.Implement;

public class ProductService : BaseService<ProductService>, IProductService
{
    private readonly HtmlSanitizerUtils _sanitizer;
    private readonly GoogleUtils.GoogleDriveService _driveService;
    private readonly SupabaseUltils _supabaseImageService;

    public ProductService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<ProductService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor, HtmlSanitizerUtils htmlSanitizer,
        GoogleUtils.GoogleDriveService driveService, SupabaseUltils supabaseImageService) : base(unitOfWork, logger,
        mapper,
        httpContextAccessor)
    {
        _sanitizer = htmlSanitizer;
        _driveService = driveService;
        _supabaseImageService = supabaseImageService;
    }

    public async Task<ApiResponse> CreateProduct(CreateProductRequest createProductRequest, Supabase.Client client)
    {
        // Lấy UserId từ HttpContext
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                            u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                            (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

        if (user == null)
        {
            throw new BadHttpRequestException("You don't have permission to do this.");
        }
        // Check SubCategory ID
        var subCategory = await _unitOfWork.GetRepository<SubCategory>()
            .SingleOrDefaultAsync(predicate: sc => sc.Id.Equals(createProductRequest.SubCategoryId),
                include: query => query.Include(sc => sc.Category));

        if (subCategory == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = MessageConstant.CategoryMessage.CategoryNotExist,
                data = null
            };
        }

        // Check product name
        var productExists = await _unitOfWork.GetRepository<Product>()
            .SingleOrDefaultAsync(predicate: p => p.ProductName.Equals(createProductRequest.ProductName));

        if (productExists != null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = MessageConstant.ProductMessage.ProductNameExisted,
                data = null
            };
        }

        // Validate quantity
        if (createProductRequest.Quantity < 0)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = MessageConstant.ProductMessage.NegativeQuantity,
                data = null
            };
        }

        // Validate images
        var validationResult = ValidateImages(createProductRequest.ImageLink);
        if (validationResult.Any())
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                listErrorMessage = validationResult,
                data = null
            };
        }

        // Sanitize description
        createProductRequest.Description = _sanitizer.Sanitize(createProductRequest.Description);

        // Create product object
        Product product = new Product
        {
            Id = Guid.NewGuid(),
            ProductName = createProductRequest.ProductName,
            SubCategoryId = createProductRequest.SubCategoryId,
            Description = createProductRequest.Description,
            CreateDate = TimeUtils.GetCurrentSEATime(),
            ModifyDate = TimeUtils.GetCurrentSEATime(),
            Price = createProductRequest.Price,
            Quantity = createProductRequest.Quantity,
            Size = createProductRequest.Size,
            Status = ProductStatusEnum.Available.GetDescriptionFromEnum(),
            Images = new List<Image>()
        };

        // Upload and associate images
        if (createProductRequest.ImageLink != null && createProductRequest.ImageLink.Any())
        {
            // Tạo một danh sách chứa IFormFile từ ImageLink
            var images = createProductRequest.ImageLink;

            try
            {
                // Tải danh sách ảnh lên Supabase và nhận về danh sách URL
                var imageUrls = await _supabaseImageService.SendImagesAsync(images, client);

                foreach (var imageUrl in imageUrls)
                {
                    // Thêm từng ảnh vào danh sách sản phẩm
                    product.Images.Add(new Image
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        CreateDate = TimeUtils.GetCurrentSEATime(),
                        ModifyDate = TimeUtils.GetCurrentSEATime(),
                        LinkImage = imageUrl
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Đã xảy ra lỗi khi tải ảnh lên Supabase: {ex.Message}");
            }
        }

        try
        {
            // Save product to database
            await _unitOfWork.GetRepository<Product>().InsertAsync(product);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (isSuccessful)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Product created successfully.",
                    data = new GetProductResponse
                    {
                        Id = product.Id,
                        Description = product.Description,
                        Images = product.Images.Select(i => i.LinkImage).ToList(),
                        ProductName = product.ProductName,
                        Quantity = product.Quantity,
                        SubCategoryName = subCategory.SubCategoryName,
                        CategoryName = subCategory.Category?.CategoryName ?? "N/A",
                        Price = product.Price,
                        Size = product.Size,
                        Status = product.Status.ToString(),
                    }
                };
            }
            else
            {
                return new ApiResponse
                {
                    status = "error",
                    message = "Failed to create product.",
                    data = null
                };
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger?.LogError(ex, "An error occurred while creating the product.");

            return new ApiResponse
            {
                status = "error",
                message = $"An error occurred: {ex.Message}",
                data = null
            };
        }
    }


    public async Task<ApiResponse> GetListProduct(int page, int size, bool? isAscending, string? SubcategoryName,
        string? productName,
        string? cateName,
        string? status,
        decimal? minPrice,
        decimal? maxPrice)
    {
        // Lấy UserId từ HttpContext
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                            u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                            (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

        if (user == null)
        {
            throw new BadHttpRequestException("You don't have permission to do this.");
        }
        var products = await _unitOfWork.GetRepository<Product>().GetPagingListAsync(
            selector: s => new GetProductResponse
            {
                Id = s.Id,
                SubCategoryName = s.SubCategory.SubCategoryName,
                CategoryName = s.SubCategory.Category.CategoryName,
                Description = s.Description,
                Images = s.Images.Select(i => i.LinkImage).ToList(),
                ProductName = s.ProductName,
                Quantity = s.Quantity,
                Price = s.Price,
                Size = s.Size,
                Status = s.Status
            },
            include: i => i.Include(p => p.SubCategory)
                .ThenInclude(p => p.Category),
            predicate: p =>
                (string.IsNullOrEmpty(productName) || p.ProductName.Contains(productName)) &&
                (string.IsNullOrEmpty(cateName) || p.SubCategory.Category.CategoryName.Contains(cateName)) &&
                (string.IsNullOrEmpty(SubcategoryName) || p.SubCategory.SubCategoryName.Contains(SubcategoryName)) &&
                (string.IsNullOrEmpty(status) || p.Status.Equals(status)) &&
                (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                (!maxPrice.HasValue || p.Price <= maxPrice.Value),
            orderBy: q => isAscending.HasValue
                ? (isAscending.Value ? q.OrderBy(p => p.Price) : q.OrderByDescending(p => p.Price))
                : q.OrderByDescending(p => p.CreateDate),
            page: page,
            size: size
        );

        int totalItems = products.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);
        if (products == null || products.Items.Count == 0)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Products retrieved successfully.",
                data = new Paginate<Product>()
                {
                    Page = page,
                    Size = size,
                    Total = totalItems,
                    TotalPages = totalPages,
                    Items = new List<Product>()
                }
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Products retrieved successfully.",
            data = products
        };
    }


    public async Task<ApiResponse> GetAllProduct(int page, int size, bool? isAscending, string? SubcategoryName,
        string? productName,
        string? cateName,
        decimal? minPrice,
        decimal? maxPrice)
    {
        
        // Đặt giá trị mặc định cho page và size nếu không hợp lệ
        page = page > 0 ? page : 1;
        size = size > 0 ? size : 10;

        // Lấy danh sách sản phẩm
        var products = await _unitOfWork.GetRepository<Product>().GetPagingListAsync(
            selector: s => new GetProductResponse
            {
                Id = s.Id,
                SubCategoryName = s.SubCategory.SubCategoryName,
                CategoryName = s.SubCategory.Category.CategoryName,
                Description = s.Description,
                Images = s.Images.Select(i => i.LinkImage).ToList(),
                ProductName = s.ProductName,
                Quantity = s.Quantity,
                Price = s.Price,
                Size = s.Size,
                Status = s.Status
            },
            include: i => i.Include(p => p.SubCategory)
                .ThenInclude(p => p.Category),
            predicate: p =>
                (string.IsNullOrEmpty(productName) || p.ProductName.Contains(productName)) &&
                (string.IsNullOrEmpty(cateName) || p.SubCategory.Category.CategoryName.Contains(cateName)) &&
                (string.IsNullOrEmpty(SubcategoryName) || p.SubCategory.SubCategoryName.Contains(SubcategoryName)) &&
                (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                (!maxPrice.HasValue || p.Price <= maxPrice.Value) &&
                (p.IsDelete == false) &&
                (p.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum())),
            orderBy: q => isAscending.HasValue
                ? (isAscending.Value ? q.OrderBy(p => p.Price) : q.OrderByDescending(p => p.Price))
                : q.OrderByDescending(p => p.CreateDate),
            page: page,
            size: size
        );

        // Tổng số sản phẩm và số trang
        int totalItems = products.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);

        // Tạo response
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = totalItems > 0 ? "Products retrieved successfully." : "No products found.",
            data = new Paginate<GetProductResponse>
            {
                Page = page,
                Size = size,
                Total = totalItems,
                TotalPages = totalPages,
                Items = products.Items.ToList()
            }
        };
    }


    public async Task<ApiResponse> GetAllProductsGroupedByCategory(int page, int size)
    {
        // Lấy toàn bộ danh sách sản phẩm có trạng thái "Available"
        var products = await _unitOfWork.GetRepository<Product>().GetPagingListAsync(
            selector: s => new 
            {
                CategoryName = s.SubCategory.Category.CategoryName,
                Product = new GetProductResponse
                {
                    Id = s.Id,
                    SubCategoryName = s.SubCategory.SubCategoryName,
                    CategoryName = s.SubCategory.Category.CategoryName,
                    Description = s.Description,
                    Images = s.Images.Select(i => i.LinkImage).ToList(),
                    ProductName = s.ProductName,
                    Quantity = s.Quantity,
                    Size = s.Size,
                    Price = s.Price,
                    Status = s.Status
                }
            },
            predicate: p => p.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum()),
            page: page,
            size: size
        );

        // Nhóm sản phẩm theo CategoryName
        var groupedProducts = products.Items
            .GroupBy(p => p.CategoryName)
            .Select(g => new 
            {
                CategoryName = g.Key,
                Products = g.Select(p => p.Product).ToList()
            })
            .ToList();

        int totalItems = products.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Products retrieved successfully.",
            data = new 
            {
                Page = page,
                Size = size,
                Total = totalItems,
                TotalPages = totalPages,
                Categories = groupedProducts
            }
        };
    }

    public async Task<ApiResponse> UpdateProduct(Guid productId, UpdateProductRequest updateProductRequest,
        Supabase.Client client)
    {
        // Lấy UserId từ HttpContext
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                            u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                            (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

        if (user == null)
        {
            throw new BadHttpRequestException("You don't have permission to do this.");
        }
        var existingProduct = await _unitOfWork.GetRepository<Product>()
            .SingleOrDefaultAsync(predicate: p => p.Id.Equals(productId));
        if (existingProduct == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = MessageConstant.ProductMessage.ProductNotExist, data = null
            };
        }

        // Check SubCategoryId if provided
        if (updateProductRequest.SubcategoryId.HasValue)
        {
            var cateCheck = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(predicate: c => c.Id.Equals(updateProductRequest.SubcategoryId.Value));
            if (cateCheck == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = MessageConstant.CategoryMessage.CategoryNotExist, data = null
                };
            }

            existingProduct.SubCategoryId = updateProductRequest.SubcategoryId.Value;
        }

        // Check product name if provided
        if (!string.IsNullOrEmpty(updateProductRequest.ProductName) &&
            !existingProduct.ProductName.Equals(updateProductRequest.ProductName))
        {
            var prodCheck = await _unitOfWork.GetRepository<Product>()
                .SingleOrDefaultAsync(predicate: p => p.ProductName.Equals(updateProductRequest.ProductName));
            if (prodCheck != null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = MessageConstant.ProductMessage.ProductNameExisted, data = null
                };
            }

            existingProduct.ProductName = updateProductRequest.ProductName;
        }

        if (!string.IsNullOrEmpty(updateProductRequest.Status) &&
            !existingProduct.Status.Equals(updateProductRequest.Status))
        {
            existingProduct.Status = updateProductRequest.Status;
        }

        if (updateProductRequest.Price.HasValue)
        {
            if (updateProductRequest.Price <= 0)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = MessageConstant.ProductMessage.NegativeQuantity, data = null
                };
            }

            existingProduct.Price = updateProductRequest.Price.Value;
        }

        // Check quantity if provided
        if (updateProductRequest.Quantity.HasValue)
        {
            if (updateProductRequest.Quantity < 0)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = MessageConstant.ProductMessage.NegativeQuantity, data = null
                };
            }

            existingProduct.Quantity = updateProductRequest.Quantity.Value;
        }

        // Update description if provided
        if (!string.IsNullOrEmpty(updateProductRequest.Description))
        {
            existingProduct.Description = _sanitizer.Sanitize(updateProductRequest.Description);
            ;
        }

        // Update images if provided
        if (updateProductRequest.ImageLink != null && updateProductRequest.ImageLink.Any())
        {
            var existingImages = await _unitOfWork.GetRepository<Image>()
                .GetListAsync(predicate: i => i.ProductId.Equals(existingProduct.Id));
            foreach (var img in existingImages)
            {
                _unitOfWork.GetRepository<Image>().DeleteAsync(img);
            }

            if (updateProductRequest.ImageLink != null && updateProductRequest.ImageLink.Any())
            {
                var images = updateProductRequest.ImageLink;

                var imageUrls = await _supabaseImageService.SendImagesAsync(images, client);

                foreach (var imageUrl in imageUrls)
                {
                    // Tạo đối tượng Image mới
                    var newImage = new Image
                    {
                        Id = Guid.NewGuid(),
                        ProductId = existingProduct.Id,
                        CreateDate = TimeUtils.GetCurrentSEATime(),
                        ModifyDate = TimeUtils.GetCurrentSEATime(),
                        LinkImage = imageUrl
                    };

                    // Thêm vào danh sách ảnh của sản phẩm hiện có
                    existingProduct.Images.Add(newImage);

                    // Thêm mới vào cơ sở dữ liệu
                    await _unitOfWork.GetRepository<Image>().InsertAsync(newImage);
                }
            }
        }


        // Commit changes
        _unitOfWork.GetRepository<Product>().UpdateAsync(existingProduct);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        if (isSuccessful)
        {
            var subCategory = await _unitOfWork.GetRepository<SubCategory>()
                .SingleOrDefaultAsync(predicate: c => c.Id.Equals(existingProduct.SubCategoryId));
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Product updated successfully.",
                data = new GetProductResponse()
                {
                    Id = existingProduct.Id,
                    Description = existingProduct.Description,
                    Images = existingProduct.Images.Select(i => i.LinkImage).ToList(),
                    ProductName = existingProduct.ProductName,
                    Quantity = existingProduct.Quantity,
                    SubCategoryName = subCategory.SubCategoryName,
                    Price = existingProduct.Price
                }
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(), message = "Failed to update product.",
            data = null
        };
    }


    public async Task<bool> DeleteProduct(Guid productId)
    {
        // Lấy UserId từ HttpContext
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                            u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                            (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

        if (user == null)
        {
            throw new BadHttpRequestException("You don't have permission to do this.");
        }
        if (productId == Guid.Empty)
        {
            throw new BadHttpRequestException(MessageConstant.ProductMessage.ProductIdEmpty);
        }

        // Find product
        var existingProduct = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(predicate: p =>
            p.Id.Equals(productId) && p.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum()));
        if (existingProduct == null)
        {
            return false;
        }

        // Mark as deleted
        existingProduct.IsDelete = true;
        existingProduct.Status = ProductStatusEnum.Unavailable.GetDescriptionFromEnum();
        _unitOfWork.GetRepository<Product>().UpdateAsync(existingProduct);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        if (isSuccessful)
        {
            return true;
        }

        return false;
    }

    public async Task<ApiResponse> GetProductById(Guid productId)
    {
        
        var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
            selector: s => new GetProductResponse
            {
                Id = s.Id,
                SubCategoryName = s.SubCategory.SubCategoryName,
                CategoryName = s.SubCategory.Category.CategoryName,
                Description = s.Description,
                Images = s.Images.Select(i => i.LinkImage).ToList(),
                ProductName = s.ProductName,
                Quantity = s.Quantity,
                Size = s.Size,
                Price = s.Price,

                Status = s.Status
            },
            predicate: p =>
                p.Id.Equals(productId) && p.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum()));

        if (product == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = MessageConstant.ProductMessage.ProductNotExist, data = null
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Product retrieved successfully.",
            data = product
        };
    }

    public async Task<ApiResponse> EnableProduct(Guid productId)
    {
       var prodcut = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(predicate: p => p.Id.Equals(productId));
       
         
      

           if (prodcut.IsDelete == true || prodcut.Status == ProductStatusEnum.Unavailable.GetDescriptionFromEnum())
           {
               prodcut.Status = ProductStatusEnum.Available.GetDescriptionFromEnum();
               prodcut.IsDelete = false;
               _unitOfWork.GetRepository<Product>().UpdateAsync(prodcut);
               await _unitOfWork.CommitAsync();
               return new ApiResponse()
               {
                   status = StatusCodes.Status200OK.ToString(),
                   message = "successfully enabled product.",
                   data = null
               };
           }   
           return new ApiResponse
           {
               status = StatusCodes.Status404NotFound.ToString(),
               message = MessageConstant.ProductMessage.ProductNotExist, data = null

           };
    }

    public Task<ApiResponse> UpImageForDescription(IFormFile formFile)
    {
        throw new NotImplementedException();
    }

    #region ValidateImages

    private List<string> ValidateImages(List<IFormFile> imageLinks)
    {
        var errorList = new List<string>();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var allowedContentTypes = new[] { "image/jpeg", "image/png" };


        foreach (var formFile in imageLinks)
        {
            if (!allowedContentTypes.Contains(formFile.ContentType, StringComparer.OrdinalIgnoreCase) ||
                !allowedExtensions.Contains(Path.GetExtension(formFile.FileName), StringComparer.OrdinalIgnoreCase))
            {
                errorList.Add($"File '{formFile.FileName}' is invalid. Only .jpg, .jpeg, and .png files are allowed.");
            }
        }

        return errorList;
    }

    #endregion
}