using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Payload.Response.Order;
using FTSS_API.Payload.Response.Pay.Payment;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FTSS_API.Service.Implement;

public class OrderService : BaseService<OrderService>, IOrderService
{
    private readonly Lazy<IPaymentService> _paymentService;

    public OrderService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<OrderService> logger, IMapper mapper,
        Lazy<IPaymentService> paymentService,
        IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _paymentService = paymentService;
    }

    // public async Task<ApiResponse> CreateOrder(CreateOrderRequest createOrderRequest)
    // {
    //     Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
    //     if (userId == null)
    //     {
    //         throw new BadHttpRequestException("User ID cannot be null.");
    //     }
    //
    //     // Validate if the CartItem list is empty
    //     if (createOrderRequest.CartItem == null || !createOrderRequest.CartItem.Any())
    //     {
    //         return new ApiResponse()
    //         {
    //             status = StatusCodes.Status400BadRequest.ToString(),
    //             message = "No Cart Items provided. Please add items to your cart before placing an order.",
    //             data = null
    //         };
    //     }
    //
    //     try
    //     {
    //         var cart = await _unitOfWork.GetRepository<Cart>()
    //             .SingleOrDefaultAsync(predicate: p => p.UserId.Equals(userId),
    //                 include: query => query.Include(c => c.User));
    //         var cartItems = new List<CartItem>();
    //         foreach (var cartItemId in createOrderRequest.CartItem)
    //         {
    //             var cartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(predicate: p =>
    //                 p.CartId.Equals(cart.Id)
    //                 && p.Id.Equals(cartItemId)
    //                 && p.Status.Equals(CartEnum.Available.GetDescriptionFromEnum()));
    //             if (cartItem != null)
    //             {
    //                 cartItems.Add(cartItem);
    //             }
    //         }
    //
    //         if (cartItems.Count == 0)
    //         {
    //             return new ApiResponse()
    //             {
    //                 status = StatusCodes.Status400BadRequest.ToString(),
    //                 message = "None of the Cart Items are available for checkout. Please verify your cart.",
    //                 data = null
    //             };
    //         }
    //
    //         decimal totalprice = 0;
    //         if (cartItems.Count == 0)
    //         {
    //             return new ApiResponse()
    //             {
    //                 status = StatusCodes.Status404NotFound.ToString(),
    //                 message = MessageConstant.CartMessage.CartItemIsEmpty,
    //                 data = null
    //             };
    //         }
    //
    //         Order order = new Order
    //         {
    //             Id = Guid.NewGuid(),
    //             TotalPrice = 0,
    //             CreateDate = TimeUtils.GetCurrentSEATime(),
    //             UserId = userId,
    //             Status = OrderStatus.PENDING_PAYMENT.GetDescriptionFromEnum(),
    //             Address = createOrderRequest.Address,
    //             Shipcost = createOrderRequest.ShipCost,
    //             OrderDetails = new List<OrderDetail>()
    //         };
    //
    //         // Add Order details to the order
    //         foreach (var cartItem in cartItems)
    //         {
    //             var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
    //                 predicate: p => p.Id.Equals(cartItem.ProductId));
    //             totalprice += cartItem.Quantity * (int)product.Price;
    //             var newOrderDetail = new OrderDetail
    //             {
    //                 Id = Guid.NewGuid(),
    //                 OrderId = order.Id,
    //                 ProductId = product.Id,
    //                 Quantity = cartItem.Quantity,
    //                 Price = product.Price,
    //             };
    //             order.OrderDetails.Add(newOrderDetail);
    //             await _unitOfWork.GetRepository<OrderDetail>().InsertAsync(newOrderDetail);
    //         }
    //
    //         order.TotalPrice = totalprice + createOrderRequest.ShipCost;
    //
    //         // Insert the Order into the database
    //         await _unitOfWork.GetRepository<Order>().InsertAsync(order);
    //         bool isSuccessOrder = await _unitOfWork.CommitAsync() > 0;
    //         if (!isSuccessOrder)
    //         {
    //             return new ApiResponse()
    //             {
    //                 status = StatusCodes.Status400BadRequest.ToString(),
    //                 message = MessageConstant.OrderMessage.CreateOrderFail,
    //                 data = null
    //             };
    //         }
    //
    //         // Delete CartItems with status "buyed"
    //         foreach (var cartItem in cartItems)
    //         {
    //             _unitOfWork.GetRepository<CartItem>().DeleteAsync(cartItem);
    //         }
    //
    //         await _unitOfWork.CommitAsync(); // Commit after deletion
    //
    //         // Prepare response
    //         var orderDetailsResponse = new List<CreateOrderResponse.OrderDetailCreateResponse>();
    //         foreach (var od in order.OrderDetails)
    //         {
    //             var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
    //                 predicate: p => p.Id.Equals(od.ProductId));
    //             if (product != null)
    //             {
    //                 orderDetailsResponse.Add(new CreateOrderResponse.OrderDetailCreateResponse
    //                 {
    //                     Price = od.Price,
    //                     ProductName = product.ProductName,
    //                     Quantity = od.Quantity
    //                 });
    //             }
    //         }
    //
    //         order = await _unitOfWork.GetRepository<Order>()
    //             .SingleOrDefaultAsync(predicate: p => p.Id.Equals(order.Id),
    //                 include: query => query.Include(o => o.User));
    //
    //         // Check if order or user is still null
    //
    //         if (order == null || order.User == null)
    //         {
    //             throw new Exception("Order or User information is missing.");
    //         }
    //
    //         if (string.IsNullOrEmpty(order.User.UserName) || string.IsNullOrEmpty(order.User.Email))
    //         {
    //             throw new Exception("User name or email is missing.");
    //         }
    //
    //         // if (order.ShipCost == null)
    //         // {
    //         //     throw new Exception("Ship cost is missing.");
    //         // }
    //         //
    //         // if (string.IsNullOrEmpty(order.Address))
    //         // {
    //         //     throw new Exception("Order address is missing.");
    //         // }
    //
    //         CreateOrderResponse createOrderResponse = new CreateOrderResponse
    //         {
    //             Id = order.Id,
    //             OrderDetails = orderDetailsResponse,
    //             ShipCost = createOrderRequest.ShipCost,
    //             TotalPrice = order.TotalPrice,
    //             Address = order.Address,
    //             userResponse = new CreateOrderResponse.UserResponse
    //             {
    //                 Name = order.User.UserName,
    //                 Email = order.User.Email,
    //                 PhoneNumber = order.User.PhoneNumber
    //             }
    //         };
    //
    //         return new ApiResponse()
    //         {
    //             status = StatusCodes.Status200OK.ToString(),
    //             message = MessageConstant.OrderMessage.CreateOrderSuccess,
    //             data = createOrderResponse
    //         };
    //     }
    //     catch (DbUpdateConcurrencyException ex)
    //     {
    //         foreach (var entry in ex.Entries)
    //         {
    //             if (entry.Entity is Order)
    //             {
    //                 var databaseValues = entry.GetDatabaseValues();
    //                 if (databaseValues == null)
    //                 {
    //                     throw new BadHttpRequestException("The order was deleted by another user.");
    //                 }
    //
    //                 throw new BadHttpRequestException(
    //                     "The order was updated by another user. Please refresh and try again.");
    //             }
    //             else if (entry.Entity is OrderDetail)
    //             {
    //                 throw new BadHttpRequestException("Concurrency conflict occurred for OrderDetail.");
    //             }
    //         }
    //
    //         throw;
    //     }
    //     catch (BadHttpRequestException)
    //     {
    //         throw;
    //     }
    //     catch (Exception ex)
    //     {
    //         throw new BadHttpRequestException("An unexpected error occurred while creating the order.", ex);
    //     }
    // }
    public async Task<ApiResponse> CreateOrder(CreateOrderRequest createOrderRequest)
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

        // Validate if the CartItem list is empty
        if (createOrderRequest.CartItem == null || !createOrderRequest.CartItem.Any())
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "No Cart Items provided. Please add items to your cart before placing an order.",
                data = null
            };
        }


        try
        {
            var cart = await _unitOfWork.GetRepository<Cart>()
                .SingleOrDefaultAsync(predicate: p => p.UserId.Equals(userId),
                    include: query => query.Include(c => c.User));

            if (cart == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Cart is not found",
                    data = null
                };
            }

            var cartItems = await _unitOfWork.GetRepository<CartItem>().GetListAsync(predicate: p =>
                p.CartId.Equals(cart.Id)
                && createOrderRequest.CartItem.Contains(p.Id)
                && p.IsDelete.Equals(false));

            if (cartItems == null || !cartItems.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "None of the Cart Items are available for checkout. Please verify your cart.",
                    data = null
                };
            }

            decimal totalprice = 0;
            List<Guid> productIds = cartItems.Select(x => x.ProductId).ToList();
            var products = await _unitOfWork.GetRepository<Product>()
                .GetListAsync(predicate: p => productIds.Contains(p.Id));
            var productsDict = products.ToDictionary(x => x.Id, x => x);

            Order order = new Order
            {
                Id = Guid.NewGuid(),
                TotalPrice = 0,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                UserId = userId,
                Status = OrderStatus.PROCESSING.GetDescriptionFromEnum(),
                Address = createOrderRequest.Address,
                Shipcost = createOrderRequest.ShipCost,

                //  OrderDetails = orderDetails // remove this to add later
            };


            // Voucher Implementation
            Voucher? voucher = null;
            if (createOrderRequest.VoucherId != null)
            {
                voucher = await _unitOfWork.GetRepository<Voucher>().SingleOrDefaultAsync(predicate: v =>
                    v.Id == createOrderRequest.VoucherId &&
                    v.Status.Equals(VoucherEnum.Active.GetDescriptionFromEnum()) &&
                    v.IsDelete.Equals(false) && // Use .Equals(false)
                    v.ExpiryDate >= TimeUtils.GetCurrentSEATime());

                if (voucher == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid or expired voucher ID.",
                        data = null
                    };
                }

                if (voucher.Quantity <= 0)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "This voucher has been fully used",
                        data = null
                    };
                }
            }


            //Calculate order details
            List<OrderDetail> orderDetails = new List<OrderDetail>();
            foreach (var cartItem in cartItems)
            {
                if (productsDict.ContainsKey(cartItem.ProductId))
                {
                    var product = productsDict[cartItem.ProductId];
                    if (product.Quantity < cartItem.Quantity)
                        throw new Exception(
                            $"Sản phẩm '{product.ProductName}' chỉ còn {product.Quantity} trong kho, không đủ để đặt hàng.");
                    totalprice += cartItem.Quantity * product.Price;
                    var newOrderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = cartItem.Quantity,
                        Price = product.Price,
                    };
                    orderDetails.Add(newOrderDetail);
                }
                else
                {
                    // Handle error if product not found. Maybe log it and skip or return bad request
                    _logger.LogError(
                        $"Product not found for cart item ID: {cartItem.Id} Product Id : {cartItem.ProductId}");
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "One of the Product on the cart is not found",
                        data = null
                    };
                }
            }

            if (createOrderRequest.VoucherId != null && voucher != null)
            {
                if (totalprice < voucher.MaximumOrderValue)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Your order value is less than the minimum for this voucher.",
                        data = null
                    };
                }

                // Apply the discount logic
                decimal discountAmount = 0;
                if (voucher.DiscountType.Equals(VoucherTypeEnum.Percentage.GetDescriptionFromEnum()))
                {
                    discountAmount = totalprice * (voucher.Discount / 100);
                }
                else if (voucher.DiscountType.Equals(VoucherTypeEnum.Fixed.GetDescriptionFromEnum()))
                {
                    discountAmount = voucher.Discount;
                }

                totalprice -= discountAmount;
                voucher.Quantity -= 1;
                _unitOfWork.GetRepository<Voucher>().UpdateAsync(voucher);
                await _unitOfWork.CommitAsync();

            }

            order.OrderDetails = orderDetails;
            order.TotalPrice = totalprice + createOrderRequest.ShipCost;
            // Insert the Order into the database
            await _unitOfWork.GetRepository<Order>().InsertAsync(order);

            // insert orderDetails
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);

         bool isSuccessOrder = false;
        int retryCount = 3;
        while (retryCount > 0)
        {
            try
            {
                isSuccessOrder = await _unitOfWork.CommitAsync() > 0;
                if (isSuccessOrder) break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CommitAsync failed, retrying... ({3 - retryCount + 1}/3) - Error: {ex.Message}");
                retryCount--;
                await Task.Delay(200); // Đợi 200ms trước khi thử lại
            }
        }

        if (!isSuccessOrder)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = MessageConstant.OrderMessage.CreateOrderFail,
                data = null
            };
        }

        var createPaymentRequest = new CreatePaymentRequest
        {
            OrderId = order.Id,
            PaymentMethod = createOrderRequest.PaymentMethod,
        };
        var paymentResponse = await _paymentService.Value.CreatePayment(createPaymentRequest);
        CreatePaymentResponse payment = null;
        if (paymentResponse != null && paymentResponse.status.Equals(StatusCodes.Status200OK.ToString()))
        {
            payment = paymentResponse.data as CreatePaymentResponse;
        }

        // Xóa CartItems có trạng thái "buyed"
        // await _unitOfWork.GetRepository<CartItem>().DeleteAsync(cartItems);
        // await _unitOfWork.CommitAsync(); // Commit sau khi xóa

        // Lấy thông tin chi tiết đơn hàng
        var orderDetailsResponse = new List<CreateOrderResponse.OrderDetailCreateResponse>();
        foreach (var od in order.OrderDetails)
        {
            var product = await _unitOfWork.GetRepository<Product>()
                .SingleOrDefaultAsync(predicate: p => p.Id.Equals(od.ProductId));

            if (product != null)
            {
                orderDetailsResponse.Add(new CreateOrderResponse.OrderDetailCreateResponse
                {
                    Price = od.Price,
                    ProductName = product.ProductName,
                    Quantity = od.Quantity
                });
            }
        }

        // Lấy lại thông tin order và user
        order = await _unitOfWork.GetRepository<Order>()
            .SingleOrDefaultAsync(predicate:p => p.Id.Equals(order.Id),
                include: query => query.Include(o => o.User));

        if (order == null || order.User == null)
        {
            _logger.LogError("Order or User information is missing.");
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Order or User information is missing.",
                data = null
            };
        }

        if (string.IsNullOrEmpty(order.User.UserName) || string.IsNullOrEmpty(order.User.Email))
        {
            _logger.LogError("User name or email is missing.");
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "User name or email is missing.",
                data = null
            };
        }

        var createOrderResponse = new CreateOrderResponse
        {
            Id = order.Id,
            OrderDetails = orderDetailsResponse,
            ShipCost = createOrderRequest.ShipCost,
            TotalPrice = order.TotalPrice,
            Address = order.Address,
            userResponse = new CreateOrderResponse.UserResponse
            {
                Name = order.User.UserName,
                Email = order.User.Email,
                PhoneNumber = order.User.PhoneNumber
            },
            CheckoutUrl = payment?.PaymentURL,
            Description = payment?.Description
        };

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = MessageConstant.OrderMessage.CreateOrderSuccess,
            data = createOrderResponse
        };
    }
    catch (DbUpdateConcurrencyException ex)
    {
        _logger.LogError($"Database concurrency issue: {ex.Message}");
        return new ApiResponse
        {
            status = StatusCodes.Status409Conflict.ToString(),
            message = "Database concurrency issue. Please try again.",
            data = null
        };
    }
    catch (Exception ex)
    {
        _logger.LogError($"An unexpected error occurred: {ex.Message}");
        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(),
            message = $"An unexpected error occurred while creating the order: {ex.Message}",
            data = null
        };
    }
}
public async Task<ApiResponse> UpdateOrder(Guid orderId, UpdateOrderRequest updateOrderRequest)
{
    try
    {
        var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
            predicate: o => o.Id == orderId,
            include: query => query.Include(o => o.User).Include(o => o.OrderDetails)
        );

        if (order == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Order not found.",
                data = null
            };
        }

        // Cập nhật trạng thái đơn hàng nếu có
        if (!string.IsNullOrEmpty(updateOrderRequest.Status))
        {
            if (updateOrderRequest.Status == OrderStatus.CANCELLED.ToString() && order.Status == OrderStatus.PAID.ToString())
            {
                var payment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(predicate: p => p.OrderId == orderId);
                if (payment != null)
                {
                    payment.Status = PaymentStatusEnum.Refunding.ToString();
                    _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
                }
            }

            order.Status = updateOrderRequest.Status;
        }

        _unitOfWork.GetRepository<Order>().UpdateAsync(order);
        
        // Commit tất cả thay đổi
        bool isUpdated = await _unitOfWork.CommitAsync() > 0;

        return new ApiResponse
        {
            status = isUpdated ? StatusCodes.Status200OK.ToString() : StatusCodes.Status400BadRequest.ToString(),
            message = isUpdated ? "Order updated successfully." : "Failed to update order.",
            data = true
        };
    }
    catch (Exception ex)
    {
        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(),
            message = $"An unexpected error occurred while updating the order: {ex.Message}",
            data = null
        };
    }
}

    public async Task<ApiResponse> GetListOrder(int page, int size, bool? isAscending)
    {
        try
        {
            // Truy vấn với Include để lấy đầy đủ thông tin
            var query = _unitOfWork.Context.Set<Order>()
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.SubCategory) // Bao gồm SubCategory
                .ThenInclude(sc => sc.Category) // Bao gồm Category từ SubCategory
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Images) // Bao gồm hình ảnh sản phẩm
                .AsQueryable();

            // Sắp xếp nếu cần
           
                query = isAscending.Value
                    ? query.OrderBy(o => o.CreateDate) // Sắp xếp tăng dần theo CreateDate
                    : query.OrderByDescending(o => o.CreateDate); // Sắp xếp giảm dần theo CreateDate
          

            // Phân trang
            var totalItems = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            if (!orders.Any())
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "No orders found.",
                    data = null
                };
            }

            // Chuyển đổi dữ liệu sang response
            var orderResponses = orders.Select(order => new GetOrderResponse
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                ShipCost = order.Shipcost,
                Address = order.Address,
                CreateDate = order.CreateDate,
                ModifyDate = order.ModifyDate,
                Discount = order.Voucher?.Discount ?? 0,
                Payment = new GetOrderResponse.PaymentResponse
                {
                    PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod,
                    PaymentStatus = order.Payments.FirstOrDefault()?.PaymentStatus,
                },
                // Lấy thông tin người dùng
                userResponse = new GetOrderResponse.UserResponse
                {
                    Name = order.User?.UserName,
                    Email = order.User?.Email,
                    PhoneNumber = order.User?.PhoneNumber
                },
                // Lấy thông tin chi tiết đơn hàng
                OrderDetails = order.OrderDetails.Select(od => new GetOrderResponse.OrderDetailCreateResponse
                {
                    ProductName = od.Product.ProductName,
                    Price = od.Price,
                    Quantity = od.Quantity,
                    LinkImage = od.Product.Images.FirstOrDefault()?.LinkImage ?? "NoImageAvailable",
                    SubCategoryName = od.Product.SubCategory?.SubCategoryName ?? "NoSubCategory",
                    CategoryName = od.Product.SubCategory?.Category?.CategoryName ?? "NoCategory"
                }).ToList(),
            }).ToList();

            // Tạo response kết quả
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Orders retrieved successfully.",
                data = new
                {
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = size,
                    Orders = orderResponses
                }
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "An error occurred while retrieving orders.",
                data = ex.Message
            };
        }
    }


    public async Task<ApiResponse> GetAllOrder(int page, int size, string status, bool? isAscending)
    {
        try
        {
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u =>
                    u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

            if (user == null)
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status401Unauthorized.ToString(),
                    message = "Unauthorized: Token is missing or expired.",
                    data = null
                };
            }

            var query = _unitOfWork.Context.Set<Order>()
                .Where(o => o.UserId == userId && (string.IsNullOrEmpty(status) || o.Status.Equals(status)))
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.SubCategory) // Bao gồm SubCategory từ Product
                .ThenInclude(sc => sc.Category) // Bao gồm Category từ SubCategory
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Images) // Bao gồm hình ảnh sản phẩm
                .AsQueryable();

            // Sắp xếp nếu cần
           
                query = isAscending.Value
                    ? query.OrderBy(o => o.CreateDate)
                    : query.OrderByDescending(o => o.CreateDate);
          
            // Phân trang
            var totalItems = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            if (!orders.Any())
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "No orders found for the specified user.",
                    data = null
                };
            }

            // Chuyển đổi dữ liệu sang response
            var orderResponses = orders.Select(order => new GetOrderResponse
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                ShipCost = order.Shipcost,
                Address = order.Address,
                CreateDate = order.CreateDate,
                ModifyDate = order.ModifyDate,
                Discount = order.Voucher?.Discount ?? 0,
                Payment = new GetOrderResponse.PaymentResponse
                {
                    PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod,
                    PaymentStatus = order.Payments.FirstOrDefault()?.PaymentStatus,
                },
                userResponse = new GetOrderResponse.UserResponse
                {
                    Name = order.User?.UserName,
                    Email = order.User?.Email,
                    PhoneNumber = order.User?.PhoneNumber
                },
                OrderDetails = order.OrderDetails.Select(od => new GetOrderResponse.OrderDetailCreateResponse
                {
                    ProductName = od.Product.ProductName,
                    Price = od.Price,
                    Quantity = od.Quantity,
                    LinkImage = od.Product.Images.FirstOrDefault()?.LinkImage ?? "NoImageAvailable",
                    SubCategoryName = od.Product.SubCategory?.SubCategoryName ?? "NoSubCategory",
                    CategoryName = od.Product.SubCategory?.Category?.CategoryName ?? "NoCategory"
                }).ToList()
            }).ToList();

            // Tạo response kết quả
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Orders retrieved successfully.",
                data = new
                {
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = size,
                    Orders = orderResponses
                }
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "An error occurred while retrieving orders.",
                data = ex.Message
            };
        }
    }


    public async Task<ApiResponse> GetOrderById(Guid id)
    {
        try
        {
            // Truy vấn để lấy thông tin đơn hàng
            var order = await _unitOfWork.Context.Set<Order>()
                .Include(o => o.User) // Bao gồm thông tin người dùng
                .Include(o => o.Voucher) // Bao gồm thông tin voucher
                .Include(o => o.Payments) // Bao gồm thông tin thanh toán
                .Include(o => o.OrderDetails) // Bao gồm chi tiết đơn hàng
                .ThenInclude(od => od.Product) // Bao gồm thông tin sản phẩm
                .ThenInclude(p => p.SubCategory) // Bao gồm SubCategory từ Product
                .ThenInclude(sc => sc.Category) // Bao gồm Category từ SubCategory
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Images) // Bao gồm hình ảnh của sản phẩm
                .FirstOrDefaultAsync(o => o.Id == id);


            if (order == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Order not found.",
                    data = null
                };
            }

            // Chuyển đổi dữ liệu sang response
            var orderResponse = new GetOrderResponse
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                ShipCost = order.Shipcost,
                Address = order.Address,
                CreateDate = order.CreateDate,
                ModifyDate = order.ModifyDate,
                Discount = order.Voucher?.Discount ?? 0,
                Payment = new GetOrderResponse.PaymentResponse
                {
                    PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod,
                    PaymentStatus = order.Payments.FirstOrDefault()?.PaymentStatus,
                },
                userResponse = new GetOrderResponse.UserResponse
                {
                    Name = order.User?.UserName,
                    Email = order.User?.Email,
                    PhoneNumber = order.User?.PhoneNumber
                },
                OrderDetails = order.OrderDetails.Select(od => new GetOrderResponse.OrderDetailCreateResponse
                {
                    ProductName = od.Product.ProductName,
                    CategoryName = od.Product.SubCategory.Category.CategoryName,
                    SubCategoryName = od.Product.SubCategory.SubCategoryName,
                    Price = od.Price,
                    Quantity = od.Quantity,
                    LinkImage = od.Product.Images.FirstOrDefault()?.LinkImage ?? "NoImageAvailable"
                }).ToList()
            };

            // Tạo response kết quả
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Order retrieved successfully.",
                data = orderResponse
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = $"An unexpected error occurred while creating the order: {ex.Message}",
                data = null
            };
        }
    }


    public Task<ApiResponse> CancelOrder(Guid id)
    {
        throw new NotImplementedException();
    }
}