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
        public CartService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<Cart> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<ApiResponse> AddCartItem(List<AddCartItemRequest> addCartItemRequest)
        {
            if (addCartItemRequest == null || !addCartItemRequest.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Request cannot be empty",
                    data = null
                };
            }
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete.Equals(false));

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
                if (!Enum.TryParse(item.Status, out CartItemEnum cartStatus) || (cartStatus != CartItemEnum.Odd && cartStatus != CartItemEnum.Setup))
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid status value. Allowed values: Odd, Setup",
                        data = null
                    };
                }

                var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                    predicate: p => p.Id.Equals(item.ProductId) && p.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

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
                    predicate: ci => ci.CartId.Equals(cart.Id) && ci.ProductId.Equals(product.Id) && ci.Status == CartItemEnum.Odd.ToString() && ci.IsDelete == false);

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
                status = isSuccessfully ? StatusCodes.Status200OK.ToString() : StatusCodes.Status500InternalServerError.ToString(),
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



        public async Task<ApiResponse> ClearCart()
        {
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

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
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

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
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

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
                                c.Product.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum()), // Kiểm tra Status của Product
                include: c => c.Include(c => c.Product),
                orderBy: c => c.OrderByDescending(o => o.CreateDate));

            if (cartItems == null || !cartItems.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "CartItem list",
                    data = new LinkedList<CartItem>()
                };
            }

            // Tạo response bao gồm LinkImage
            var response = new List<GetAllCartItemResponse>();

            foreach (var cartItem in cartItems)
            {
                // Lấy LinkImage từ bảng Image theo ProductId
                var image = await _unitOfWork.GetRepository<Image>().SingleOrDefaultAsync(
                    predicate: img => img.ProductId.Equals(cartItem.ProductId) && img.IsDelete == false,
                    orderBy: img => img.OrderBy(i => i.CreateDate));

                response.Add(new GetAllCartItemResponse
                {
                    CartItemId = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.ProductName,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Product.Price,
                    Price = cartItem.Product.Price * cartItem.Quantity,
                    LinkImage = image?.LinkImage // Sử dụng LinkImage nếu tồn tại, nếu không để null
                });
            }

            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "CartItem list",
                data = response
            };
        }



        public async Task<ApiResponse> GetCartSummary()
        {
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            if (userId == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status401Unauthorized.ToString(),
                    message = "Unauthorized: Token is missing or expired.",
                    data = null
                };
            }

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

            if (user == null)
            {
                throw new BadHttpRequestException("User not available.");
            }

            var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(
                predicate: c => c.UserId.Equals(userId));


            var cartItems = await _unitOfWork.GetRepository<CartItem>().GetListAsync(
                    predicate: c => c.CartId.Equals(cart.Id) &&
                    c.IsDelete.Equals(false) &&
                    c.Product.Status.Equals(ProductStatusEnum.Available.GetDescriptionFromEnum()), // Thêm điều kiện lọc Product
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
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

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



            existingCartItem.Quantity = updateCartItemRequest.Quantity.HasValue ? updateCartItemRequest.Quantity.Value
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
