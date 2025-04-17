using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Request.Order;
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Payload.Request.Return;
using FTSS_API.Payload.Response.Order;
using FTSS_API.Payload.Response.Pay.Payment;
using FTSS_API.Payload.Response.SetupPackage;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediaType = FTSS_Model.Enum.MediaType;

namespace FTSS_API.Service.Implement;

public class OrderService : BaseService<OrderService>, IOrderService
{
    private readonly Lazy<IPaymentService> _paymentService;
    private readonly IEmailSender _emailSender;
    private readonly SupabaseUltils _supabaseImageService;

    public OrderService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<OrderService> logger, IMapper mapper,
        Lazy<IPaymentService> paymentService,
        IEmailSender emailSender,
        SupabaseUltils supabaseImageService,
        IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _paymentService = paymentService;
        _emailSender = emailSender;
        _supabaseImageService = supabaseImageService;
    }


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

        // Validate if either SetupPackageId or CartItem is provided
        if ((createOrderRequest.SetupPackageId == null || createOrderRequest.SetupPackageId == Guid.Empty) &&
            (createOrderRequest.CartItem == null || !createOrderRequest.CartItem.Any()))
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Either Setup Package ID or Cart Items must be provided for placing an order.",
                data = null
            };
        }

        try
        {
            List<OrderDetail> orderDetails = new List<OrderDetail>();
            decimal totalProductPrice = 0;
            // Initialize the new isEligible flag (default to false)
            bool isEligible = false;
            Order order = new Order
            {
                Id = Guid.NewGuid(),
                TotalPrice = 0,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                UserId = userId,
                Status = OrderStatus.PROCESSING.GetDescriptionFromEnum(),
                Address = createOrderRequest.Address,
                Shipcost = createOrderRequest.ShipCost,
                PhoneNumber = createOrderRequest.PhoneNumber,
                RecipientName = createOrderRequest.RecipientName,
                VoucherId = createOrderRequest.VoucherId,
                InstallationDate = createOrderRequest.InstallationDate,
                SetupPackageId = createOrderRequest.SetupPackageId,
               
                IsEligible = false,
                IsAssigned = false,
                OrderCode = GenerateOrderCode().Trim()
            };

            // Branch the flow based on whether we're using SetupPackageId or CartItem
            if (createOrderRequest.SetupPackageId != null && createOrderRequest.SetupPackageId != Guid.Empty)
            {
                // Process order from Setup Package
                var setupPackage = await _unitOfWork.GetRepository<SetupPackage>()
                    .SingleOrDefaultAsync(predicate: p =>
                        p.Id.Equals(createOrderRequest.SetupPackageId) && p.IsDelete.Equals(false));

                if (setupPackage == null)
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Setup Package not found or has been deleted.",
                        data = null
                    };
                }

                // Get setup package items
                var setupItems = await _unitOfWork.GetRepository<SetupPackageDetail>()
                    .GetListAsync(predicate: si => si.SetupPackageId.Equals(setupPackage.Id));

                if (setupItems == null || !setupItems.Any())
                {
                    return new ApiResponse()
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "The selected Setup Package does not contain any items.",
                        data = null
                    };
                }

                // Get all product IDs from setup items
                List<Guid> productIds = setupItems.Select(x => x.ProductId).ToList();
                var products = await _unitOfWork.GetRepository<Product>()
                    .GetListAsync(predicate: p => productIds.Contains(p.Id) && p.IsDelete.Equals(false));

                var productsDict = products.ToDictionary(x => x.Id, x => x);

                // Create order details from setup items
                foreach (var setupItem in setupItems)
                {
                    if (productsDict.ContainsKey(setupItem.ProductId))
                    {
                        var product = productsDict[setupItem.ProductId];
                        if (product.Quantity < setupItem.Quantity)
                        {
                            return new ApiResponse()
                            {
                                status = StatusCodes.Status400BadRequest.ToString(),
                                message =
                                    $"Sản phẩm '{product.ProductName}' chỉ còn {product.Quantity} trong kho, không đủ để đặt hàng.",
                                data = null
                            };
                        }

                        decimal itemPrice = (decimal)setupItem.Quantity * product.Price;
                        totalProductPrice += itemPrice;

                        var newOrderDetail = new OrderDetail
                        {
                            Id = Guid.NewGuid(),
                            OrderId = order.Id,
                            ProductId = product.Id,
                            Quantity = (int)setupItem.Quantity,
                            Price = product.Price,
                        };
                        orderDetails.Add(newOrderDetail);

                        // Update product quantity
                        product.Quantity -= setupItem.Quantity;
                        _unitOfWork.GetRepository<Product>().UpdateAsync(product);
                    }
                    else
                    {
                        return new ApiResponse()
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "One of the products in the setup package is not found",
                            data = null
                        };
                    }
                }

                // Check if totalProductPrice is at least 2,000,000 for setup packages
                // and set isEligible flag
                const decimal eligibilityThreshold = 2000000; // 2 million VND
                isEligible = totalProductPrice >= eligibilityThreshold;
            }
            else // Process order from Cart Items
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

                // Get all product IDs from cart items
                List<Guid> productIds = cartItems.Select(x => x.ProductId).ToList();
                var products = await _unitOfWork.GetRepository<Product>()
                    .GetListAsync(predicate: p => productIds.Contains(p.Id) && p.IsDelete.Equals(false));

                var productsDict = products.ToDictionary(x => x.Id, x => x);

                // Create order details from cart items
                foreach (var cartItem in cartItems)
                {
                    if (productsDict.ContainsKey(cartItem.ProductId))
                    {
                        var product = productsDict[cartItem.ProductId];
                        if (product.Quantity < cartItem.Quantity)
                        {
                            return new ApiResponse()
                            {
                                status = StatusCodes.Status400BadRequest.ToString(),
                                message =
                                    $"Sản phẩm '{product.ProductName}' chỉ còn {product.Quantity} trong kho, không đủ để đặt hàng.",
                                data = null
                            };
                        }

                        decimal itemPrice = cartItem.Quantity * product.Price;
                        totalProductPrice += itemPrice;

                        var newOrderDetail = new OrderDetail
                        {
                            Id = Guid.NewGuid(),
                            OrderId = order.Id,
                            ProductId = product.Id,
                            Quantity = cartItem.Quantity,
                            Price = product.Price,
                        };
                        orderDetails.Add(newOrderDetail);

                        // Update product quantity
                        product.Quantity -= cartItem.Quantity;
                        _unitOfWork.GetRepository<Product>().UpdateAsync(product);
                    }
                    else
                    {
                        _logger.LogError(
                            $"Product not found for cart item ID: {cartItem.Id} Product Id: {cartItem.ProductId}");
                        return new ApiResponse()
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "One of the Products in the cart is not found",
                            data = null
                        };
                    }
                }

                isEligible = false;
            }

            order.IsEligible = isEligible;
            // No order details created - issue with products
            if (!orderDetails.Any())
            {
                return new ApiResponse()
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Could not create order details. Please check product availability.",
                    data = null
                };
            }

            // Apply voucher if provided
            decimal discountAmount = 0;
            if (createOrderRequest.VoucherId != null)
            {
                var voucher = await _unitOfWork.GetRepository<Voucher>().SingleOrDefaultAsync(predicate: v =>
                    v.Id == createOrderRequest.VoucherId &&
                    v.Status.Equals(VoucherEnum.Active.GetDescriptionFromEnum()) &&
                    v.IsDelete.Equals(false) &&
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


                // Apply the discount logic
                if (voucher.DiscountType.Equals(VoucherTypeEnum.Percentage.GetDescriptionFromEnum()))
                {
                    discountAmount = totalProductPrice * (voucher.Discount / 100);
                    discountAmount = Math.Min(discountAmount, (decimal)voucher.MaximumOrderValue);
                }
                else if (voucher.DiscountType.Trim().Equals(VoucherTypeEnum.Fixed.GetDescriptionFromEnum()))
                {
                    discountAmount = voucher.Discount;
                    discountAmount = Math.Min(discountAmount, (decimal)voucher.MaximumOrderValue);
                }

// Đảm bảo giảm giá không vượt quá tổng giá sản phẩm
                discountAmount = Math.Min(discountAmount, totalProductPrice);

// Update voucher usage
                voucher.Quantity -= 1;
                 _unitOfWork.GetRepository<Voucher>().UpdateAsync(voucher);

            }

            // Calculate final price
            decimal finalPrice = totalProductPrice - discountAmount + createOrderRequest.ShipCost;
            order.TotalPrice = finalPrice;
            order.OrderDetails = orderDetails;

            // Insert the Order into the database
            await _unitOfWork.GetRepository<Order>().InsertAsync(order);

            // Insert order details
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);

            // Commit with retry
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
                    await Task.Delay(200); // Wait 200ms before retry
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

            // Create payment
            var createPaymentRequest = new CreatePaymentRequest
            {
                OrderId = order.Id,
                PaymentMethod = createOrderRequest.PaymentMethod,
            };
            var paymentResponse = await _paymentService.Value.CreatePayment(createPaymentRequest);

// Xử lý payment response mới - không cần cast
            string paymentUrl = string.Empty;
            string paymentDescription = string.Empty;

            if (paymentResponse != null && paymentResponse.status.Equals(StatusCodes.Status200OK.ToString()))
            {
                // Cách 1: Dùng dynamic access
                try
                {
                    dynamic paymentData = paymentResponse.data;
                    paymentUrl = paymentData?.PaymentURL ?? paymentData?.paymentUrl; // Xử lý cả camelCase và PascalCase
                    paymentDescription = paymentData?.Description ?? paymentData?.description;
                }
                catch
                {
                    // Cách 2: Dùng dictionary nếu dynamic không work
                    if (paymentResponse.data is Dictionary<string, object> dict)
                    {
                        paymentUrl = dict.TryGetValue("PaymentURL", out var url) ? url.ToString()
                            : dict.TryGetValue("paymentUrl", out var url2) ? url2.ToString() : "";
                        paymentDescription = dict.TryGetValue("Description", out var desc) ? desc.ToString() : "";
                    }
                }
            }

            // Prepare order details for response
            var orderDetailsResponse = new List<CreateOrderResponse.OrderDetailCreateResponse>();
            foreach (var od in orderDetails)
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

            // Get updated order and user info
            order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: p => p.Id.Equals(order.Id),
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

            // Build response
            var createOrderResponse = new CreateOrderResponse
            {
                Id = order.Id,
                OrderDetails = orderDetailsResponse,
                ShipCost = createOrderRequest.ShipCost,
                TotalPrice = order.TotalPrice,
                Address = order.Address,
                RecipientName = order.RecipientName,
                PhoneNumber = order.PhoneNumber,
                OrderCode = order.OrderCode,
                SetupPackageId = order.SetupPackageId,
                IsEligible = order.IsEligible,
                userResponse = new CreateOrderResponse.UserResponse
                {
                    Name = order.User.UserName,
                    Email = order.User.Email,
                    PhoneNumber = order.User.PhoneNumber
                },
                CheckoutUrl = paymentUrl,
                Description = paymentDescription
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
                include: query => query.Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.Payments)
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

            // Update order status if provided
            if (!string.IsNullOrEmpty(updateOrderRequest.Status))
            {
                // Check if the status is being updated to CANCELLED
                if (updateOrderRequest.Status == OrderStatus.CANCELLED.ToString())
                {
                    var payment = order.Payments?.FirstOrDefault();
                    bool isPaid = payment != null && payment.PaymentStatus == PaymentStatusEnum.Completed.ToString();
                    string emailBody = EmailTemplatesUtils.RefundNotificationEmailTemplate(order.Id, order.OrderCode, isPaid);
                    var email = order.User.Email;
                    await _emailSender.SendRefundNotificationEmailAsync(email, emailBody);
                }
                else if (updateOrderRequest.Status == OrderStatus.RETURN_ACCEPTED.ToString())
                {
                    string returnEmailBody = EmailTemplatesUtils.ReturnAcceptedEmailTemplate(order.Id, order.OrderCode);
                    var customerEmail = order.User.Email;
                    await _emailSender.SendReturnAcceptedEmailAsync(customerEmail, returnEmailBody);
                }

                order.Status = updateOrderRequest.Status;
                order.ModifyDate = DateTime.Now;
                _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            }

            // Commit all changes
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

    public async Task<ApiResponse> GetListOrder(int page, int size, bool? isAscending, string orderCode)
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
                .ThenInclude(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Images) // ✅ Hình ảnh sản phẩm trong OrderDetails
                .Include(o => o.SetupPackage) // ✅ Đảm bảo lấy SetupPackage
                .ThenInclude(sp => sp.SetupPackageDetails)
                .ThenInclude(spd => spd.Product)
                .ThenInclude(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(o => o.SetupPackage)
                .ThenInclude(sp => sp.SetupPackageDetails)
                .ThenInclude(spd => spd.Product)
                .ThenInclude(p => p.Images) // ✅ Hình ảnh sản phẩm trong SetupPackage
                .AsQueryable();

            // Sắp xếp nếu cần
            if (!isAscending.HasValue) isAscending = true;
            query = isAscending.Value
                ? query.OrderBy(o => o.CreateDate) // Sắp xếp tăng dần theo CreateDate
                : query.OrderByDescending(o => o.CreateDate); // Sắp xếp giảm dần theo CreateDate

            if (orderCode != null)
            {
                query = query.Where(o => o.OrderCode == orderCode);
            }

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
                    status = StatusCodes.Status200OK.ToString(),
                    message = "No orders found.",
                    data = new List<Order>()
                };
            }

            // Chuyển đổi dữ liệu sang response
           var orderResponses = orders.Select(order =>
        {
            decimal discountAmount = 0;

            if (order.Voucher != null)
            {
                if (order.Voucher.DiscountType.Equals(VoucherTypeEnum.Percentage.GetDescriptionFromEnum()))
                {
                    discountAmount = order.TotalPrice * (order.Voucher.Discount / 100);
                    discountAmount = Math.Min(discountAmount, (decimal)order.Voucher.MaximumOrderValue);
                }
                else if (order.Voucher.DiscountType.Trim().Equals(VoucherTypeEnum.Fixed.GetDescriptionFromEnum()))
                {
                    discountAmount = order.Voucher.Discount;
                    discountAmount = Math.Min(discountAmount, (decimal)order.Voucher.MaximumOrderValue);
                }
            }

            return new GetOrderResponse
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                ShipCost = order.Shipcost,
                Address = order.Address,
                CreateDate = order.CreateDate,
                OderCode = order.OrderCode,
                IsAssigned = order.IsAssigned,
                IsEligible = order.IsEligible,
                ModifyDate = order.ModifyDate,
                PhoneNumber = order.PhoneNumber,
                BuyerName = order.RecipientName,
                InstallationDate = order.InstallationDate,

                SetupPackage = order.SetupPackage != null
                    ? new SetupPackageResponse()
                    {
                        SetupPackageId = order.SetupPackageId,
                        SetupName = order.SetupPackage?.SetupName,
                        ModifyDate = order.ModifyDate,
                        Size = order.SetupPackage?.SetupPackageDetails?
                            .Where(spd => spd.Product?.SubCategory?.Category?.CategoryName == "Bể")
                            .Select(spd => spd.Product?.Size)
                            .FirstOrDefault(),
                        CreateDate = order.CreateDate,
                        TotalPrice = order.SetupPackage?.Price ?? 0,
                        Description = order.SetupPackage?.Description ?? "N/A",
                        IsDelete = order.SetupPackage?.IsDelete ?? false,
                        Products = order.SetupPackage?.SetupPackageDetails?.Select(spd => new ProductResponse
                        {
                            Id = spd.Product?.Id ?? Guid.Empty,
                            ProductName = spd.Product?.ProductName ?? "Unknown",
                            Quantity = spd.Quantity,
                            Price = spd.Product?.Price ?? 0,
                            InventoryQuantity = spd.Product?.Quantity ?? 0,
                            Status = spd.Product != null ? spd.Product.Status : "false",
                            IsDelete = order.SetupPackage?.IsDelete ?? false,
                            CategoryName = spd.Product?.SubCategory?.Category?.CategoryName ?? "Unknown",
                            images = spd.Product?.Images?
                                .Where(img => img.IsDelete == false)
                                .OrderBy(img => img.CreateDate)
                                .Select(img => img.LinkImage)
                                .FirstOrDefault() ?? "NoImageAvailable"
                        }).ToList() ?? new List<ProductResponse>()
                    }
                    : null,

                Payment = order.Payments != null && order.Payments.Any()
                    ? new GetOrderResponse.PaymentResponse
                    {
                        PaymentId = order.Payments.FirstOrDefault()?.Id ?? Guid.Empty,
                        PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod ?? "Unknown",
                        PaymentStatus = order.Payments.FirstOrDefault()?.PaymentStatus ?? "Unknown",
                        BankHolder = order.Payments.FirstOrDefault()?.BankHolder ?? "Unknown",
                        BankName = order.Payments.FirstOrDefault()?.BankName ?? "Unknown",
                        BankNumber = order.Payments.FirstOrDefault()?.BankNumber ?? "Unknown",
                    }
                    : null,

                userResponse = order.User != null
                    ? new GetOrderResponse.UserResponse
                    {
                        Name = order.User.UserName ?? "Unknown",
                        Email = order.User.Email ?? "Unknown",
                        PhoneNumber = order.User.PhoneNumber ?? "Unknown"
                    }
                    : null,

                Voucher = order.Voucher != null ? new GetOrderResponse.VoucherResponse()
                {
                    VoucherCode = order.Voucher?.VoucherCode,
                    DiscountType = order.Voucher?.DiscountType,
                    Discount = order.Voucher?.Discount,
                    MaximumOrderValue = order.Voucher?.MaximumOrderValue,
                } : null,

                OrderDetails = order.OrderDetails?.Select(od => new GetOrderResponse.OrderDetailCreateResponse
                {
                    ProductName = od.Product?.ProductName ?? "Unknown",
                    Price = od.Price,
                    Quantity = od.Quantity,
                    LinkImage = od.Product?.Images?.FirstOrDefault()?.LinkImage ?? "NoImageAvailable",
                    SubCategoryName = od.Product?.SubCategory?.SubCategoryName ?? "NoSubCategory",
                    CategoryName = od.Product?.SubCategory?.Category?.CategoryName ?? "NoCategory"
                }).ToList() ?? new List<GetOrderResponse.OrderDetailCreateResponse>()
            };
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



public async Task<ApiResponse> GetAllOrder(int page, int size, string status, string orderCode, bool? isAscending)
{
    try
    {
        // Xác thực người dùng
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var userExists = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                          u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

        if (userExists == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status401Unauthorized.ToString(),
                message = "Unauthorized: Token is missing or expired.",
                data = null
            };
        }

        // Đếm tổng số bản ghi trước
        var countQuery = _unitOfWork.Context.Set<Order>()
            .Where(x =>
                (string.IsNullOrEmpty(status) || x.Status.Equals(status)) &&
                (string.IsNullOrEmpty(orderCode) || x.OrderCode.Contains(orderCode)));
                
        var totalItems = await countQuery.CountAsync();
        
        // Nếu không có dữ liệu, trả về sớm
        if (totalItems == 0)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "No orders found for the specified criteria.",
                data = new { 
                    TotalItems = 0,
                    Page = page,
                    PageSize = size,
                    Orders = new List<GetOrderResponse>()
                }
            };
        }

        // Tối ưu hóa query chính
        var query = _unitOfWork.Context.Set<Order>()
            .Where(x =>
                (string.IsNullOrEmpty(status) || x.Status.Equals(status)) &&
                (string.IsNullOrEmpty(orderCode) || x.OrderCode.Contains(orderCode)))
            .Include(o => o.User)
            .Include(o => o.Voucher)
            .Include(o => o.Payments)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.SetupPackage)
                .ThenInclude(sp => sp.SetupPackageDetails)
                    .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category)
            .Include(o => o.SetupPackage)
                .ThenInclude(sp => sp.SetupPackageDetails)
                    .ThenInclude(spd => spd.Product)
                        .ThenInclude(p => p.Images)
            .AsSplitQuery(); // Thêm AsSplitQuery để tối ưu hiệu suất khi có nhiều Include

        // Áp dụng sắp xếp
        if (!isAscending.HasValue) isAscending = true;
        query = isAscending.Value
            ? query.OrderBy(o => o.CreateDate)
            : query.OrderByDescending(o => o.CreateDate);

        // Phân trang
        var orders = await query
            .Skip((page - 1) * size)
            .Take(size)
            .AsNoTracking()
            .ToListAsync();

        // Sử dụng AutoMapper để mapping
        var orderResponses = _mapper.Map<List<GetOrderResponse>>(orders);

        // Xử lý thêm logic tính toán phức tạp nếu cần
        foreach (var orderResponse in orderResponses)
        {
            if (orders.FirstOrDefault(o => o.Id == orderResponse.Id)?.Voucher != null)
            {
                var order = orders.First(o => o.Id == orderResponse.Id);
                var voucher = order.Voucher;
                
                if (voucher.DiscountType.Equals(VoucherTypeEnum.Percentage.GetDescriptionFromEnum()))
                {
                    var discountAmount = order.TotalPrice * (voucher.Discount / 100);
                    discountAmount = Math.Min(discountAmount, (decimal)voucher.MaximumOrderValue);
                    // Nếu cần thiết, cập nhật orderResponse với thông tin này
                }
                else if (voucher.DiscountType.Trim().Equals(VoucherTypeEnum.Fixed.GetDescriptionFromEnum()))
                {
                    var discountAmount = voucher.Discount;
                    discountAmount = Math.Min(discountAmount, (decimal)voucher.MaximumOrderValue);
                    // Cập nhật orderResponse nếu cần
                }
            }
        }

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
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.Payments)
                .Include(o => o.ReturnRequests)
                .ThenInclude(rr => rr.ReturnRequestMedia)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.SubCategory) // Bao gồm SubCategory
                .ThenInclude(sc => sc.Category) // Bao gồm Category từ SubCategory
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Images) // Bao gồm hình ảnh sản phẩm
                .Include(o => o.SetupPackage) // Bao gồm SetupPackage
                .ThenInclude(sp => sp.SetupPackageDetails) // Bao gồm SetupPackageDetails
                .ThenInclude(spd => spd.Product) // Bao gồm Product trong SetupPackageDetails
                .ThenInclude(p => p.SubCategory) // Bao gồm SubCategory
                .ThenInclude(sc => sc.Category) // Bao gồm Category từ SubCategory
                .Include(o => o.SetupPackage)
                .ThenInclude(sp => sp.SetupPackageDetails)
                .ThenInclude(spd => spd.Product)
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
            // Tính toán số tiền giảm giá
            decimal discountAmount = 0;
            
            if (order.Voucher != null)
            {
                // Apply the discount logic
                if (order.Voucher.DiscountType.Equals(VoucherTypeEnum.Percentage.GetDescriptionFromEnum()))
                {
                    discountAmount = order.TotalPrice * (order.Voucher.Discount / 100);
                    discountAmount = Math.Min(discountAmount, (decimal)order.Voucher.MaximumOrderValue);
                }
                else if (order.Voucher.DiscountType.Trim().Equals(VoucherTypeEnum.Fixed.GetDescriptionFromEnum()))
                {
                    discountAmount = order.Voucher.Discount;
                    discountAmount = Math.Min(discountAmount, (decimal)order.Voucher.MaximumOrderValue);
                }
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
                IsEligible = order.IsEligible,
                OderCode = order.OrderCode,
                IsAssigned = order.IsAssigned,
                ModifyDate = order.ModifyDate,
                PhoneNumber = order.PhoneNumber,
                BuyerName = order.RecipientName,
                InstallationDate = order.InstallationDate,
              
                Voucher = order.Voucher != null ? new GetOrderResponse.VoucherResponse()
                {
                    Discount = order.Voucher?.Discount,
                    VoucherCode = order.Voucher?.VoucherCode,
                    DiscountType = order.Voucher?.DiscountType,
                    MaximumOrderValue = order.Voucher?.MaximumOrderValue,
                } : null,
                SetupPackage = order.SetupPackage != null
                    ? new SetupPackageResponse()
                    {
                        SetupPackageId = order.SetupPackageId,
                        SetupName = order.SetupPackage?.SetupName,
                        ModifyDate = order.ModifyDate,
                        Size = order.SetupPackage?.SetupPackageDetails?
                            .Where(spd => spd.Product?.SubCategory?.Category?.CategoryName == "Bể")
                            .Select(spd => spd.Product?.Size)
                            .FirstOrDefault(),
                        CreateDate = order.CreateDate,
                        TotalPrice = order.SetupPackage?.Price ?? 0,
                        Description = order.SetupPackage?.Description ?? "N/A",
                        IsDelete = order.SetupPackage?.IsDelete ?? false,
                        Products = order.SetupPackage?.SetupPackageDetails?.Select(spd => new ProductResponse
                        {
                            Id = spd.Product?.Id ?? Guid.Empty,
                            ProductName = spd.Product?.ProductName ?? "Unknown",
                            Quantity = spd.Quantity,
                            Price = spd.Product?.Price ?? 0,
                            InventoryQuantity = spd.Product?.Quantity ?? 0,
                            Status = spd.Product != null ? spd.Product.Status : "false",
                            IsDelete = order.SetupPackage?.IsDelete ?? false,
                            CategoryName = spd.Product?.SubCategory?.Category?.CategoryName ?? "Unknown",
                            images = spd.Product?.Images?
                                .Where(img => img.IsDelete == false)
                                .OrderBy(img => img.CreateDate)
                                .Select(img => img.LinkImage)
                                .FirstOrDefault() ?? "NoImageAvailable"
                        }).ToList() ?? new List<ProductResponse>()
                    }
                    : null,
                Payment = new GetOrderResponse.PaymentResponse
                {
                    PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod,
                    PaymentId = order.Payments.FirstOrDefault()?.Id ?? Guid.Empty,
                    PaymentStatus = order.Payments.FirstOrDefault()?.PaymentStatus,
                    BankHolder = order.Payments.FirstOrDefault()?.BankHolder ?? "Unknown",
                    BankName = order.Payments.FirstOrDefault()?.BankName ?? "Unknown",
                    BankNumber = order.Payments.FirstOrDefault()?.BankNumber ?? "Unknown",
                },
                userResponse = new GetOrderResponse.UserResponse
                {
                    Name = order.User?.UserName,
                    Email = order.User?.Email,
                    PhoneNumber = order.User?.PhoneNumber
                },
                ReturnRequests  = order.ReturnRequests?.Where(rr => rr.IsDelete == false).Select(rr => new GetOrderResponse.ReturnRequestResponse
                {
                    Id = rr.Id,
                    Reason = rr.Reason,
                    Status = rr.Status,
                    CreatedAt = rr.CreatedAt,
                    MediaFiles = rr.ReturnRequestMedia?.Where(m => m.IsDelete == false).Select(m => new GetOrderResponse.ReturnRequestMediaResponse
                    {
                        Id = m.Id,
                        MediaLink = m.MediaLink,
                        MediaType = m.MediaType
                    }).ToList() ?? new List<GetOrderResponse.ReturnRequestMediaResponse>()
                }).ToList() ?? new List<GetOrderResponse.ReturnRequestResponse>(),

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
public async Task<ApiResponse> CreateReturnRequest(CreateReturnRequest request, Supabase.Client client)
{
    try
    {
        // Kiểm tra người dùng
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                           u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                           u.IsDelete == false &&
                           u.Role.Equals(RoleEnum.Customer.GetDescriptionFromEnum()));

        if (user == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status403Forbidden.ToString(),
                message = "Bạn không có quyền thực hiện thao tác này. Chỉ khách hàng mới có thể gửi yêu cầu hoàn hàng.",
                data = null
            };
        }

        // Kiểm tra đơn hàng
        var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
            predicate: o => o.Id == request.OrderId && o.IsDelete == false,
            include: query => query.Include(o => o.Payments));

        if (order == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Không tìm thấy đơn hàng hoặc đơn hàng đã bị xóa.",
                data = null
            };
        }

        // Kiểm tra đơn hàng thuộc về người dùng
        if (order.UserId != userId)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status403Forbidden.ToString(),
                message = "Bạn không có quyền gửi yêu cầu hoàn hàng cho đơn hàng này.",
                data = null
            };
        }

        // Kiểm tra trạng thái thanh toán và trạng thái đơn hàng
        var payment = order.Payments?.FirstOrDefault();
        if (payment == null || payment.PaymentStatus != PaymentStatusEnum.Completed.ToString())
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Đơn hàng chưa được thanh toán hoàn tất. Trạng thái thanh toán phải là 'completed' để gửi yêu cầu hoàn hàng.",
                data = null
            };
        }

        if (order.Status != OrderStatus.COMPLETED.ToString())
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Đơn hàng chưa hoàn tất giao hàng. Trạng thái đơn hàng phải là 'completed' để gửi yêu cầu hoàn hàng.",
                data = null
            };
        }

        // Kiểm tra xem đã có yêu cầu hoàn hàng nào cho đơn hàng này chưa
        var existingRequest = await _unitOfWork.GetRepository<ReturnRequest>().SingleOrDefaultAsync(
            predicate: r => r.OrderId == request.OrderId && r.IsDelete == false);

        if (existingRequest != null && existingRequest.Status == ReturnRequestStatusEnum.PENDING.ToString())
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Đã có một yêu cầu hoàn hàng đang chờ xử lý cho đơn hàng này.",
                data = null
            };
        }

        // Kiểm tra lý do
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Lý do hoàn hàng phải có ít nhất 10 ký tự.",
                data = null
            };
        }

        // Kiểm tra và tải file ảnh/video lên Supabase
        if (request.MediaFiles == null || !request.MediaFiles.Any())
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Cần cung cấp ít nhất một ảnh hoặc video minh chứng.",
                data = null
            };
        }

        // Validate định dạng file
        var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var allowedVideoExtensions = new[] { ".mp4", ".mov", ".avi" };
        var maxFileSize = 50 * 1024 * 1024; // 50MB

        foreach (var file in request.MediaFiles)
        {
            if (file.Length == 0)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Một hoặc nhiều file rỗng. Vui lòng kiểm tra lại.",
                    data = null
                };
            }

            if (file.Length > maxFileSize)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Kích thước file không được vượt quá 50MB.",
                    data = null
                };
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedImageExtensions.Contains(fileExtension) && !allowedVideoExtensions.Contains(fileExtension))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Định dạng file không được hỗ trợ. Chỉ chấp nhận ảnh (JPG, JPEG, PNG) hoặc video (MP4, MOV, AVI).",
                    data = null
                };
            }
        }

        // Tải các file lên Supabase
        var mediaUrls = await _supabaseImageService.SendImagesAsync(request.MediaFiles, client);
        if (mediaUrls == null || mediaUrls.Count != request.MediaFiles.Count)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Không thể tải một hoặc nhiều file lên. Vui lòng thử lại.",
                data = null
            };
        }

        // Tạo yêu cầu hoàn hàng mới
        var returnRequest = new ReturnRequest
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = userId.Value,
            Reason = request.Reason,
            Status = ReturnRequestStatusEnum.PENDING.ToString(),
            CreatedAt = TimeUtils.GetCurrentSEATime(),
            IsDelete = false,
            ReturnRequestMedia = new List<ReturnRequestMedium>()
        };

        // Tạo danh sách media
        for (int i = 0; i < request.MediaFiles.Count; i++)
        {
            var file = request.MediaFiles[i];
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var mediaType = allowedImageExtensions.Contains(fileExtension) ? MediaType.IMAGE.ToString() : MediaType.VIDEO.ToString();

            returnRequest.ReturnRequestMedia.Add(new ReturnRequestMedium
            {
                Id = Guid.NewGuid(),
                ReturnRequestId = returnRequest.Id,
                MediaLink = mediaUrls[i],
                MediaType = mediaType,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                IsDelete = false
            });
        }

        await _unitOfWork.GetRepository<ReturnRequest>().InsertAsync(returnRequest);
        order.Status = OrderStatus.RETURNING.ToString();
         _unitOfWork.GetRepository<Order>().UpdateAsync(order);
        // Lưu thay đổi
        bool isSuccess = await _unitOfWork.CommitAsync() > 0;
        if (!isSuccess)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Không thể lưu yêu cầu hoàn hàng. Vui lòng thử lại.",
                data = null
            };
        }

        // Gửi email thông báo cho manager
        var managers = await _unitOfWork.GetRepository<User>().GetListAsync(
            predicate: u => u.Role.Equals(RoleEnum.Manager.GetDescriptionFromEnum()) &&
                           u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                           u.IsDelete == false);

        foreach (var manager in managers)
        {
            if (!string.IsNullOrEmpty(manager.Email))
            {
                // Tạo danh sách liên kết ảnh/video trong email
                var mediaLinksHtml = string.Join("", returnRequest.ReturnRequestMedia.Select(m => 
                    $"<p>{m.MediaType}: <a href='{m.MediaLink}'>Xem {m.MediaType.ToLower()}</a></p>"));

                string emailBody = $@"<h1>Thông Báo Yêu Cầu Hoàn Hàng</h1>
                                     <p>Khách hàng {user.UserName} đã gửi một yêu cầu hoàn hàng cho đơn hàng mã {order.OrderCode} (ID: {order.Id}).</p>
                                     <p>Lý do: {request.Reason}</p>
                                     <p>Minh chứng:</p>
                                     {mediaLinksHtml}
                                     <p>Vui lòng xem xét và xử lý yêu cầu.</p>";
                await _emailSender.SendRefundNotificationEmailAsync(manager.Email, emailBody);
            }
        }

        return new ApiResponse
        {
            status = StatusCodes.Status201Created.ToString(),
            message = "Yêu cầu hoàn hàng đã được gửi thành công. Manager sẽ xem xét yêu cầu của bạn.",
            data = new
            {
                ReturnRequestId = returnRequest.Id,
                OrderId = returnRequest.OrderId,
                Reason = returnRequest.Reason,
                Status = returnRequest.Status,
                CreatedAt = returnRequest.CreatedAt,
                Media = returnRequest.ReturnRequestMedia.Select(m => new
                {
                    MediaLink = m.MediaLink,
                    MediaType = m.MediaType
                }).ToList()
            }
        };
    }
    catch (Exception ex)
    {
        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(),
            message = $"Đã xảy ra lỗi khi gửi yêu cầu hoàn hàng: {ex.Message}",
            data = null
        };
    }
}

public async Task<ApiResponse> GetReturnRequest(Guid? returnRequestId, Supabase.Client client, int page, int size)
{
   

    try
    {
        // Kiểm tra người dùng
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) &&
                            u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                            u.IsDelete == false);

        if (user == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status403Forbidden.ToString(),
                message = "Bạn không có quyền thực hiện thao tác này.",
                data = null
            };
        }

        bool isCustomer = user.Role.Equals(RoleEnum.Customer.GetDescriptionFromEnum());
        bool isManager = user.Role.Equals(RoleEnum.Manager.GetDescriptionFromEnum());

        if (!isCustomer && !isManager)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status403Forbidden.ToString(),
                message = "Chỉ khách hàng hoặc quản lý mới có thể xem yêu cầu hoàn hàng.",
                data = null
            };
        }

        // Nếu có returnRequestId, lấy chi tiết một yêu cầu hoàn hàng
        if (returnRequestId.HasValue)
        {
            var returnRequest = await _unitOfWork.GetRepository<ReturnRequest>().SingleOrDefaultAsync(
                predicate: r => r.Id == returnRequestId && r.IsDelete == false,
                include: query => query
                    .Include(r => r.Order)
                    .Include(r => r.ReturnRequestMedia.Where(m => m.IsDelete == false)));

            if (returnRequest == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Không tìm thấy yêu cầu hoàn hàng hoặc yêu cầu đã bị xóa.",
                    data = null
                };
            }

            // Kiểm tra quyền truy cập
            if (isCustomer && returnRequest.UserId != userId)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status403Forbidden.ToString(),
                    message = "Bạn không có quyền xem yêu cầu hoàn hàng này.",
                    data = null
                };
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Lấy thông tin yêu cầu hoàn hàng thành công.",
                data = new
                {
                    ReturnRequestId = returnRequest.Id,
                    OrderId = returnRequest.OrderId,
                    OrderCode = returnRequest.Order?.OrderCode,
                    UserId = returnRequest.UserId,
                    Reason = returnRequest.Reason,
                    Status = returnRequest.Status,
                    CreatedAt = returnRequest.CreatedAt,
                    Media = returnRequest.ReturnRequestMedia.Select(m => new
                    {
                        MediaLink = m.MediaLink,
                        MediaType = m.MediaType
                    }).ToList()
                }
            };
        }

        // Lấy danh sách yêu cầu hoàn hàng
   
        int pageSize = size > 0 ? size : 10; // Default page size if not specified
        int currentPage = page > 0 ? page : 1; // Default to first page if not specified

       
    

        var returnRequests = await _unitOfWork.GetRepository<ReturnRequest>().GetPagingListAsync(
            selector: r => new
            {
                ReturnRequestId = r.Id,
                OrderId = r.OrderId,
                OrderCode = r.Order != null ? r.Order.OrderCode : null,
                UserId = r.UserId,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                Media = r.ReturnRequestMedia
                    .Where(m => m.IsDelete == false)
                    .Select(m => new
                    {
                        MediaLink = m.MediaLink,
                        MediaType = m.MediaType
                    }).ToList()
            },
            predicate: r => r.IsDelete == false && (isCustomer ? r.UserId == userId : true),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt),
            size: pageSize,
            page: currentPage
        );
        var totalItems = returnRequests.Total;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
// If no return requests found
        if (returnRequests == null || returnRequests.Items.Count == 0)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Không có yêu cầu hoàn hàng nào.",
                data = new
                {
                    TotalCount = 0,
                    Page = currentPage,
                    PageSize = pageSize,
                    TotalPages = 0,
                    ReturnRequests = new List<object>()
                }
            };
        }
        // Trả về ApiResponse
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Lấy danh sách yêu cầu hoàn trả thành công.",
            data = new
            {
                TotalCount = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                ReturnRequests = returnRequests.Items
            }
        };
    }
    catch (Exception ex)
    {
        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(),
            message = $"Đã xảy ra lỗi khi lấy thông tin yêu cầu hoàn trả: {ex.Message}",
            data = null
        };
    }
}

#region tạo orderCode

    private string GenerateOrderCode()
    {
        var now = DateTime.UtcNow; // Lấy thời gian hiện tại theo UTC để tránh trùng lặp
        string timestamp = now.ToString("yyyyMMddHHmmssfff"); // VD: 20250322153045999
        string randomLetters = GenerateRandomLetters(7).Trim(); // Loại bỏ khoảng trắng
        string lastThreeDigits = timestamp[^3..]; // Lấy 3 số cuối của timestamp

        return $"{randomLetters}{lastThreeDigits}"; // VD: ABC999
    }

// Hàm tạo 3 chữ cái ngẫu nhiên
    private string GenerateRandomLetters(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    #endregion

    public Task<ApiResponse> CancelOrder(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> UpdateTime(Guid id, UpdateTimeRequest request)
    {
        try
        {
            // Lấy đơn hàng
            var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: o => o.Id.Equals(id) && o.IsDelete == false);

            if (order == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status404NotFound.ToString(),
                    message = "Không tìm thấy đơn hàng.",
                    data = null
                };
            }

            var currentSEATime = TimeUtils.GetCurrentSEATime();

            // Nếu có truyền ngày lắp đặt thì mới xử lý
            if (request.InstallationDate.HasValue)
            {
                var installationDate = request.InstallationDate.Value;
                if (order.InstallationDate != installationDate)
                {
                    order.InstallationDate = installationDate;
                    order.ModifyDate = currentSEATime;

                    _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                    await _unitOfWork.CommitAsync();
                }
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Cập nhật thời gian lắp đặt thành công.",
                data = null
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Đã xảy ra lỗi khi cập nhật thời gian lắp đặt.",
                data = ex.Message
            };
        }
    }
}
