using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.Order;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FTSS_API.Service.Implement;

public class OrderService : BaseService<OrderService>, IOrderService
{
    public OrderService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<OrderService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
    }

    public async Task<ApiResponse> CreateOrder(CreateOrderRequest createOrderRequest)
    {
         Guid?userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
 if (userId == null)
 {
     throw new BadHttpRequestException("User ID cannot be null.");
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
     var cart = await _unitOfWork.GetRepository<Cart>().SingleOrDefaultAsync(predicate: p => p.UserId.Equals(userId), include: query => query.Include(c => c.User));
     var cartItems = new List<CartItem>();
     foreach (var cartItemId in createOrderRequest.CartItem)
     {
         var cartItem = await _unitOfWork.GetRepository<CartItem>().SingleOrDefaultAsync(predicate: p => p.CartId.Equals(cart.Id)
                                                                                             && p.Id.Equals(cartItemId)
                                                                                             && p.Status.Equals(CartEnum.Available.GetDescriptionFromEnum()));
         if (cartItem != null)
         {
             cartItems.Add(cartItem);
         }
     }
     if (cartItems.Count == 0)
     {
         return new ApiResponse()
         {
             status = StatusCodes.Status400BadRequest.ToString(),
             message = "None of the Cart Items are available for checkout. Please verify your cart.",
             data = null
         };
     }
     decimal totalprice = 0;
     if (cartItems.Count == 0)
     {
         return new ApiResponse()
         {
             status = StatusCodes.Status404NotFound.ToString(),
             message = MessageConstant.CartMessage.CartItemIsEmpty,
             data = null
         };
     }
     Order order = new Order
     {
         Id = Guid.NewGuid(),
         TotalPrice = 0,
         CreateDate = TimeUtils.GetCurrentSEATime(),
         UserId = userId,
         Status = OrderStatus.PENDING_PAYMENT.GetDescriptionFromEnum(),
         Address = createOrderRequest.Address,
         Shipcost = createOrderRequest.ShipCost,
         OrderDetails = new List<OrderDetail>()
     };

     // Add Order details to the order
     foreach (var cartItem in cartItems)
     {
         var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
            predicate: p => p.Id.Equals(cartItem.ProductId));
         totalprice += cartItem.Quantity * (int)product.Price;
         var newOrderDetail = new OrderDetail
         {
             Id = Guid.NewGuid(),
             OrderId = order.Id,
             ProductId = product.Id,
             Quantity = cartItem.Quantity,
             Price = product.Price,
         };
         order.OrderDetails.Add(newOrderDetail);
         await _unitOfWork.GetRepository<OrderDetail>().InsertAsync(newOrderDetail);
     }
     order.TotalPrice = totalprice + createOrderRequest.ShipCost;

     // Insert the Order into the database
     await _unitOfWork.GetRepository<Order>().InsertAsync(order);
     bool isSuccessOrder = await _unitOfWork.CommitAsync() > 0;
     if (!isSuccessOrder)
     {
         return new ApiResponse()
         {
             status = StatusCodes.Status400BadRequest.ToString(),
             message = MessageConstant.OrderMessage.CreateOrderFail,
             data = null
         };
     }

     // Delete CartItems with status "buyed"
     foreach (var cartItem in cartItems)
     {
         _unitOfWork.GetRepository<CartItem>().DeleteAsync(cartItem);
     }
     await _unitOfWork.CommitAsync(); // Commit after deletion

     // Prepare response
     var orderDetailsResponse = new List<CreateOrderResponse.OrderDetailCreateResponse>();
     foreach (var od in order.OrderDetails)
     {
         var product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
             predicate: p => p.Id.Equals(od.ProductId));
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
     order = await _unitOfWork.GetRepository<Order>()
.SingleOrDefaultAsync(predicate: p => p.Id.Equals(order.Id), include: query => query.Include(o => o.User));

     // Check if order or user is still null

     if (order == null || order.User == null)
     {
         throw new Exception("Order or User information is missing.");
     }

     if (string.IsNullOrEmpty(order.User.UserName) || string.IsNullOrEmpty(order.User.Email))
     {
         throw new Exception("User name or email is missing.");
     }

     // if (order.ShipCost == null)
     // {
     //     throw new Exception("Ship cost is missing.");
     // }
     //
     // if (string.IsNullOrEmpty(order.Address))
     // {
     //     throw new Exception("Order address is missing.");
     // }

     CreateOrderResponse createOrderResponse = new CreateOrderResponse
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
         }
     };

     return new ApiResponse()
     {
         status = StatusCodes.Status200OK.ToString(),
         message = MessageConstant.OrderMessage.CreateOrderSuccess,
         data = createOrderResponse
     };
 }
 catch (DbUpdateConcurrencyException ex)
 {
     foreach (var entry in ex.Entries)
     {
         if (entry.Entity is Order)
         {
             var databaseValues = entry.GetDatabaseValues();
             if (databaseValues == null)
             {
                 throw new BadHttpRequestException("The order was deleted by another user.");
             }
             throw new BadHttpRequestException("The order was updated by another user. Please refresh and try again.");
         }
         else if (entry.Entity is OrderDetail)
         {
             throw new BadHttpRequestException("Concurrency conflict occurred for OrderDetail.");
         }
     }
     throw;
 }
 catch (BadHttpRequestException)
 {
     throw;
 }
 catch (Exception ex)
 {
     throw new BadHttpRequestException("An unexpected error occurred while creating the order.", ex);
 }
    }

    public Task<ApiResponse> GetListOrder(int page, int size, bool? isAscending)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse> GetAllOrder(int page, int size, string status, bool? isAscending, string userName)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse> GetOrderById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse> CancelOrder(Guid id)
    {
        throw new NotImplementedException();
    }
}