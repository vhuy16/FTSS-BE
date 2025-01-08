using FTSS_API.Payload;
using FTSS_API.Payload.Request.CartItem;

namespace FTSS_API.Service.Interface
{
    public interface ICartService
    {
        Task<ApiResponse> AddCartItem(AddCartItemRequest addCartItemRequest);
        Task<ApiResponse> DeleteCartItem(Guid ItemId);
        Task<ApiResponse> GetAllCartItem();
        Task<ApiResponse> ClearCart();
        Task<ApiResponse> GetCartSummary();
        Task<ApiResponse> UpdateCartItem(Guid id, UpdateCartItemRequest updateCartItemRequest);
    }
}
