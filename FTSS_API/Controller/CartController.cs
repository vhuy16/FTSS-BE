﻿using FTSS_API.Constant;
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

        [HttpPost(ApiEndPointConstant.Cart.AddCartItem)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AddCartItem([FromBody] AddCartItemRequest addCartItemRequest)
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

        [HttpDelete(ApiEndPointConstant.Cart.DeleteCartItem)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCartItem([FromRoute] Guid itemId)
        {
            var response = await _cartService.DeleteCartItem(itemId);
            return StatusCode(int.Parse(response.status), response);
        }

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

        [HttpDelete(ApiEndPointConstant.Cart.ClearCart)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> ClearAllCart()
        {
            var response = await _cartService.ClearCart();
            return StatusCode(int.Parse(response.status), response);
        }

        [HttpGet(ApiEndPointConstant.Cart.GetCartSummary)]
        [ProducesResponseType(typeof(CartSummayResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetCartSummary()
        {
            var response = await _cartService.GetCartSummary();
            return Ok(response);
        }

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
    }
}
