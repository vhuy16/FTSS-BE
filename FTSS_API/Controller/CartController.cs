using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.CartItem;
using FTSS_API.Payload.Response.CartItem;
using FTSS_API.Service.Interface;
using FTSS_Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller
{
    public class CartController : BaseController<CartController>
    {
        private readonly ICartService _cartService;
        public CartController(ILogger<CartController> logger, ICartService cartService) : base(logger)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// API thêm sản phẩm vào cart.
        /// </summary>
        [HttpPost(ApiEndPointConstant.Cart.AddCartItem)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AddCartItem([FromBody] List<AddCartItemRequest> addCartItemRequest)
        {
            var addCartItemResponse = await _cartService.AddCartItem(addCartItemRequest);
            if (addCartItemResponse.status == StatusCodes.Status404NotFound.ToString())
            {
                return NotFound(addCartItemResponse);
            }

            if (addCartItemResponse.status == StatusCodes.Status400BadRequest.ToString())
            {
                return BadRequest(addCartItemResponse);
            }
            return CreatedAtAction(nameof(AddCartItem), addCartItemResponse);
        }

        /// <summary>
        /// API xoá sản phẩm khỏi cart.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Cart.DeleteCartItem)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCartItem([FromRoute] Guid itemId)
        {
            var response = await _cartService.DeleteCartItem(itemId);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy thông tin tất cả sản phẩm trong cart.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Cart.GetAllCart)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllCart()
        {
            var response = await _cartService.GetAllCartItem();
            if (response.data == null)
            {
                response.data = new List<CartItem>();
            }
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API xóa tất cả sản phẩm trong cart.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Cart.ClearCart)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> ClearAllCart()
        {
            var response = await _cartService.ClearCart();
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy thông tin số sản phẩm và giá tiền.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Cart.GetCartSummary)]
        [ProducesResponseType(typeof(CartSummayResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetCartSummary()
        {
            var response = await _cartService.GetCartSummary();
            return Ok(response);
        }

        /// <summary>
        /// API cập nhật số lượng sản phẩm trong cart.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Cart.UpdateCartItem)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCartItem([FromRoute] Guid itemId, [FromBody] UpdateCartItemRequest updateCartItemRequest)
        {
            var response = await _cartService.UpdateCartItem(itemId, updateCartItemRequest);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API thêm gói cài đặt vào cart.
        /// </summary>
        [HttpPost(ApiEndPointConstant.Cart.AddSetupPackageToCart)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AddSetupPackageToCart([FromBody] Guid setupPackageId)
        {
            var response = await _cartService.AddSetupPackageToCart(setupPackageId);
            if (response.status == StatusCodes.Status404NotFound.ToString())
            {
                return NotFound(response);
            }
            if (response.status == StatusCodes.Status400BadRequest.ToString())
            {
                return BadRequest(response);
            }
            return CreatedAtAction(nameof(AddSetupPackageToCart), response);
        }
    }
}
