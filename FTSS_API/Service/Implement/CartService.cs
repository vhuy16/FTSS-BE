using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload.Request.CartItem;
using FTSS_API.Payload;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Repository.Interface;
using FTSS_Model.Enum;
using FTSS_API.Payload.Response.CartItem;
using Microsoft.EntityFrameworkCore;


namespace FTSS_API.Service.Implement
{
    public class CartService : BaseService<Cart>, ICartService
    {
        public CartService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<Cart> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<ApiResponse> AddCartItem(List<AddCartItemRequest> addCartItemRequest)
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete == false &&
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

            if (addCartItemRequest == null || !addCartItemRequest.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Request cannot be empty",
                    data = null
                };
            }

            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));

            if (cart == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Cart not found",
                    data = null
                };
            }

            List<AddCartItemResponse> cartItemResponses = new List<AddCartItemResponse>();

            foreach (var item in addCartItemRequest)
            {
                if (!Enum.TryParse(item.Status, out CartItemEnum cartStatus) ||
                    (cartStatus != CartItemEnum.Odd && cartStatus != CartItemEnum.Setup))
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid status value. Allowed values: Odd, Setup",
                        data = null
                    };
                }

                var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                    predicate: p =>
                        p.Id.Equals(item.ProductId) &&
                        p.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

                if (product == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = MessageConstant.ProductMessage.ProductNotExist,
                        data = null
                    };
                }

                if (item.Quantity <= 0)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = MessageConstant.CartMessage.NegativeQuantity,
                        data = null
                    };
                }

                var existingCartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(
                    predicate: ci =>
                        ci.CartId.Equals(cart.Id) && ci.ProductId.Equals(product.Id) &&
                        ci.Status == CartItemEnum.Odd.ToString() && ci.IsDelete == false);

                if (existingCartItem != null)
                {
                    // Nếu sản phẩm đã tồn tại trong giỏ hàng với trạng thái Odd, cộng thêm số lượng
                    int newQuantity = existingCartItem.Quantity + item.Quantity;

                    if (newQuantity > product.Quantity)
                    {
                        return new ApiResponse()
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = MessageConstant.ProductMessage.ProductNotEnough,
                            data = null
                        };
                    }

                    existingCartItem.Quantity = newQuantity;
                    existingCartItem.ModifyDate = TimeUtils.GetCurrentSEATime();
                    _unitOfWork.GetRepository<CartItem>().UpdateAsync(existingCartItem);
                }
                else
                {
                    // Nếu sản phẩm chưa có trong giỏ hàng, thêm mới
                    if (item.Quantity > product.Quantity)
                    {
                        return new ApiResponse()
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = MessageConstant.ProductMessage.ProductNotEnough,
                            data = null
                        };
                    }

                    var cartItem = new CartItem()
                    {
                        Id = Guid.NewGuid(),
                        CartId = cart.Id,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        Status = cartStatus.ToString(),
                        CreateDate = TimeUtils.GetCurrentSEATime(),
                        ModifyDate = TimeUtils.GetCurrentSEATime()
                    };

                    await _unitOfWork.GetRepository<CartItem>().InsertAsync(cartItem);
                }

                cartItemResponses.Add(new AddCartItemResponse()
                {
                    CartItemId = existingCartItem?.Id ?? Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Price = product.Price * item.Quantity,
                });
            }

            bool isSuccessfully = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse()
            {
                status = isSuccessfully
                    ? StatusCodes.Status200OK.ToString()
                    : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessfully ? "Add cart items successful" : "Failed to add cart items",
                data = cartItemResponses
            };
        }


        //public async Task<ApiResponse> AddCartItem(AddCartItemRequest addCartItemRequest)
        //{
        //    Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        //    var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
        //        predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete.Equals(false));

        //    if (user == null)
        //    {
        //        throw new BadHttpRequestException("You need log in.");
        //    }

        //    var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
        //        predicate: c => c.UserId.Equals(userId));

        //    var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
        //        predicate: p => p.Id.Equals(addCartItemRequest.ProductId) && p.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

        //    if (product == null)
        //    {
        //        return new ApiResponse()
        //        {
        //            status = StatusCodes.Status404NotFound.ToString(),
        //            message = MessageConstant.ProductMessage.ProductNotExist,
        //            data = null
        //        };
        //    }

        //    if (product.Quantity < addCartItemRequest.Quantity)
        //    {
        //        return new ApiResponse()
        //        {
        //            status = StatusCodes.Status400BadRequest.ToString(),
        //            message = MessageConstant.ProductMessage.ProductNotEnough,
        //            data = null
        //        };
        //    }

        //    if (addCartItemRequest.Quantity <= 0)
        //    {
        //        return new ApiResponse()
        //        {
        //            status = StatusCodes.Status400BadRequest.ToString(),
        //            message = MessageConstant.CartMessage.NegativeQuantity,
        //            data = null
        //        };
        //    }

        //    CartItem cartItem;
        //    var existingCartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(
        //        predicate: ci => ci.CartId.Equals(cart.Id) && ci.ProductId.Equals(addCartItemRequest.ProductId)
        //        && ci.Status.Equals(CartEnum.Available.GetDescriptionFromEnum()));

        //    if (existingCartItem != null)
        //    {
        //        existingCartItem.Quantity += addCartItemRequest.Quantity;
        //        existingCartItem.ModifyDate = TimeUtils.GetCurrentSEATime();

        //        if (product.Quantity < existingCartItem.Quantity)
        //        {
        //            throw new BadHttpRequestException(MessageConstant.ProductMessage.ProductNotEnough);
        //        }

        //        _unitOfWork.GetRepository<CartItem>().UpdateAsync(existingCartItem);
        //        cartItem = existingCartItem; // Assign the existing cart item
        //    }
        //    else
        //    {
        //        cartItem = new CartItem()
        //        {
        //            Id = Guid.NewGuid(),
        //            CartId = cart.Id,
        //            ProductId = product.Id,
        //            Quantity = addCartItemRequest.Quantity,
        //            Status = CartEnum.Available.GetDescriptionFromEnum(),
        //            CreateDate = TimeUtils.GetCurrentSEATime(),
        //            ModifyDate = TimeUtils.GetCurrentSEATime()
        //        };

        //        await _unitOfWork.GetRepository<CartItem>().InsertAsync(cartItem);
        //    }

        //    bool isSuccessfully = await _unitOfWork.CommitAsync() > 0;
        //    AddCartItemResponse? addCartItemResponse = null;
        //    if (isSuccessfully)
        //    {
        //        addCartItemResponse = new AddCartItemResponse()
        //        {
        //            CartItemId = cartItem.Id, // Always use the CartItem.Id
        //            ProductId = product.Id,
        //            ProductName = product.ProductName,
        //            Price = product.Price * addCartItemRequest.Quantity,
        //        };
        //    }

        //    return new ApiResponse()
        //    {
        //        status = StatusCodes.Status200OK.ToString(),
        //        message = "Add cart item successful",
        //        data = addCartItemResponse
        //    };
        //}


        public async Task<ApiResponse> AddSetupPackageToCart(Guid setupPackageId)
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete != true && u.Role == RoleEnum.Customer.GetDescriptionFromEnum());

            if (user == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status401Unauthorized.ToString(),
                    message = "Unauthorized: Token is missing or expired.",
                    data = null
                };
            }

            var cart = await _unitOfWork.GetRepository<Cart>()
                .SingleOrDefaultAsync(predicate: c => c.UserId.Equals(userId));

            if (cart == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Cart not found",
                    data = null
                };
            }

            // Lấy danh sách sản phẩm từ setupPackageId
            var packageProducts = await _unitOfWork.GetRepository<SetupPackageDetail>()
                .GetListAsync(predicate: spd => spd.SetupPackageId.Equals(setupPackageId));

            if (packageProducts == null || !packageProducts.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Setup package not found or contains no products",
                    data = null
                };
            }

            // Lấy thông tin gói setup
            var setupPackage = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                predicate: sp => sp.Id.Equals(setupPackageId));

            if (setupPackage == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Setup package not found",
                    data = null
                };
            }

            List<AddCartItemResponse> cartItemResponses = new List<AddCartItemResponse>();

            foreach (var packageItem in packageProducts)
            {
                var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                    include: x => x.Include(x => x.Images),
                    predicate: p => p.Id.Equals(packageItem.ProductId) &&
                                    p.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

                if (product == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = $"Product {packageItem.ProductId} does not exist or is unavailable",
                        data = null
                    };
                }

                if (packageItem.Quantity <= 0)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid product quantity in setup package",
                        data = null
                    };
                }

                var existingCartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(
                    predicate: ci => ci.CartId.Equals(cart.Id) && ci.ProductId.Equals(product.Id) &&
                                     ci.Status == CartItemEnum.Odd.ToString() && ci.IsDelete != true);

                if (existingCartItem != null)
                {
                    // Nếu sản phẩm đã tồn tại trong giỏ hàng với trạng thái Odd, cộng thêm số lượng
                    int? newQuantity = existingCartItem.Quantity + packageItem.Quantity;

                    if (newQuantity > product.Quantity)
                    {
                        return new ApiResponse()
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = $"Not enough stock for product {product.ProductName}",
                            data = null
                        };
                    }

                    existingCartItem.Quantity = (int)newQuantity;
                    existingCartItem.ModifyDate = TimeUtils.GetCurrentSEATime();
                    _unitOfWork.GetRepository<CartItem>().UpdateAsync(existingCartItem);
                }
                else
                {
                    // Nếu sản phẩm chưa có trong giỏ hàng, thêm mới
                    if (packageItem.Quantity > product.Quantity)
                    {
                        return new ApiResponse()
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = $"Not enough stock for product {product.ProductName}",
                            data = null
                        };
                    }

                    var cartItem = new CartItem()
                    {
                        Id = Guid.NewGuid(),
                        CartId = cart.Id,
                        ProductId = product.Id,
                        Quantity = (int)packageItem.Quantity,
                        Status = CartItemEnum.Setup.ToString(),
                        CreateDate = TimeUtils.GetCurrentSEATime(),
                        ModifyDate = TimeUtils.GetCurrentSEATime()
                    };
                }

                cartItemResponses.Add(new AddCartItemResponse()
                {
                    CartItemId = existingCartItem?.Id ?? Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Price = product.Price * packageItem.Quantity,
                    Quantity = (int)packageItem.Quantity,
                    UnitPrice = product.Price,
                    LinkImage = product.Images.FirstOrDefault()?.LinkImage
                });
            }

            bool isSuccessfully = await _unitOfWork.CommitAsync() > 0;

            return new ApiResponse()
            {
                status = isSuccessfully
                    ? StatusCodes.Status200OK.ToString()
                    : StatusCodes.Status500InternalServerError.ToString(),
                message = isSuccessfully ? "Setup package added to cart successfully" : "Failed to add setup package",
                data = new
                {
                    SetupId = setupPackage.Id,
                    SetupName = setupPackage.SetupName,
                    CartItems = cartItemResponses
                }
            };
        }


        public async Task<ApiResponse> ClearCart()
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete == false &&
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

            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));

            var cartItems = await _unitOfWork.GetRepository<CartItem>().GetListAsync(
                predicate: c => c.CartId.Equals(cart.Id) && c.IsDelete.Equals(false));

            if (cartItems == null || !cartItems.Any())
            {
                throw new BadHttpRequestException(MessageConstant.CartMessage.CartItemIsEmpty);
            }

            foreach (var cartItem in cartItems)
            {
                cartItem.IsDelete = true;
                _unitOfWork.GetRepository<CartItem>().UpdateAsync(cartItem);
            }

            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Clear cart successful",
                data = isSuccessful
            };
        }

        public async Task<ApiResponse> DeleteCartItem(Guid ItemId)
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete == false &&
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

            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));

            var cartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(
                predicate: c => c.CartId.Equals(cart.Id) && c.IsDelete.Equals(false) && c.Id.Equals(ItemId));

            if (cartItem == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = MessageConstant.CartMessage.CartItemNotExist,
                    data = false
                };
            }

            cartItem.IsDelete = true; // Đặt isDelete thành true thay vì thay đổi status
            cartItem.ModifyDate = TimeUtils.GetCurrentSEATime();
            _unitOfWork.GetRepository<CartItem>().UpdateAsync(cartItem);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Delete successful",
                data = isSuccessful
            };
        }


        public async Task<ApiResponse> GetAllCartItem()
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);

            // Lấy thông tin người dùng
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

            // Lấy giỏ hàng của user
            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));

            if (cart == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cart not found",
                    data = new List<GetAllCartItemResponse>()
                };
            }

            // Lấy tất cả CartItems thuộc Cart và các thông tin sản phẩm liên quan
            var cartItems = await _unitOfWork.GetRepository<CartItem>().GetListAsync(
                predicate: c => c.CartId.Equals(cart.Id) &&
                                c.IsDelete.Equals(false) &&
                                c.Product.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum()),
                include: c => c.Include(ci => ci.Product),
                orderBy: c => c.OrderByDescending(o => o.CreateDate));

            if (cartItems == null || !cartItems.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "No items in the cart",
                    data = new List<GetAllCartItemResponse>()
                };
            }

            // Lấy toàn bộ ProductId từ cartItems
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();

            // Lấy image cho toàn bộ ProductId trong cartItems chỉ trong 1 query
            var images = await _unitOfWork.GetRepository<Image>().GetListAsync(
                predicate: img => productIds.Contains(img.ProductId) && img.IsDelete == false,
                orderBy: img => img.OrderBy(i => i.CreateDate));

            // Tạo response bao gồm thông tin từ cartItems và link image
            var response = cartItems.Select(cartItem =>
            {
                var image = images.FirstOrDefault(img => img.ProductId == cartItem.ProductId);

                return new GetAllCartItemResponse
                {
                    Status = cartItem.Status,
                    CartItemId = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.ProductName,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Product.Price,
                    Price = cartItem.Product.Price * cartItem.Quantity,
                    LinkImage = image?.LinkImage
                };
            }).ToList();

            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "CartItem list",
                data = response
            };
        }


        public async Task<ApiResponse> GetCartSummary()
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete == false &&
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

            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));


            var cartItems = await _unitOfWork.GetRepository<CartItem>().GetListAsync(
                predicate: c => c.CartId.Equals(cart.Id) &&
                                c.IsDelete.Equals(false) &&
                                c.Product.Status.Equals(ProductStatusEnum.Available
                                    .GetDescriptionFromEnum()), // Thêm điều kiện lọc Product
                include: c => c.Include(c => c.Product), // Bao gồm Product để sử dụng các thuộc tính của nó
                orderBy: c => c.OrderByDescending(o => o.CreateDate));


            if (cartItems == null || !cartItems.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Summary",
                    data = new CartSummayResponse()
                    {
                        TotalItems = 0,
                        TotalPrice = 0
                    }
                };
            }


            decimal totalPrice = 0;
            int totalItems = 0;

            foreach (var cartItem in cartItems)
            {
                totalItems += cartItem.Quantity;
                totalPrice += (cartItem.Product?.Price ?? 0) * cartItem.Quantity;
            }

            var response = new CartSummayResponse()
            {
                TotalItems = totalItems,
                TotalPrice = totalPrice
            };

            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Summary",
                data = response
            };
        }


        public async Task<ApiResponse> UpdateCartItem(Guid id, UpdateCartItemRequest updateCartItemRequest)
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete == false &&
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

            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));

            var existingCartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(
                predicate: ci => ci.Id.Equals(id) && ci.CartId.Equals(cart.Id)
                                                  && ci.IsDelete.Equals(false));

            if (existingCartItem == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = MessageConstant.CartMessage.CartItemNotExist.ToString(),
                    data = null
                };
            }

            var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                predicate: p => p.Id.Equals(existingCartItem.ProductId) && p.IsDelete.Equals(false));

            if (updateCartItemRequest.Quantity <= 0)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = MessageConstant.CartMessage.NegativeQuantity.ToString(),
                    data = null
                };
            }

            if (product.Quantity < updateCartItemRequest.Quantity)
            {
                var response = new ApiResponse()
                {
                    status = StatusCodes.Status200OK.ToString(),
                    data = new UpdateCartItemResponse
                    {
                        CartItemId = id,
                        ProductId = product.Id,
                        Quantity = existingCartItem.Quantity,
                        ProductName = product.ProductName,
                        UnitPrice = product.Price,
                        Price = product.Price * existingCartItem.Quantity,
                    }
                };

                response.message = "Only have " + product.Quantity + " items";

                return response;
            }


            existingCartItem.Quantity = updateCartItemRequest.Quantity.HasValue
                ? updateCartItemRequest.Quantity.Value
                : existingCartItem.Quantity;
            existingCartItem.ModifyDate = TimeUtils.GetCurrentSEATime();
            _unitOfWork.GetRepository<CartItem>().UpdateAsync(existingCartItem);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            UpdateCartItemResponse updateCartItemResponse = new();
            if (isSuccessful)
            {
                updateCartItemResponse = new UpdateCartItemResponse()
                {
                    CartItemId = id,
                    ProductId = product.Id,
                    Quantity = updateCartItemRequest.Quantity,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Price = product.Price * updateCartItemRequest.Quantity,
                };
            }

            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "update successful",
                data = updateCartItemResponse
            };
        }
    }
}