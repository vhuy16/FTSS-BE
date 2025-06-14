﻿using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Product;
using FTSS_API.Payload.Response;
using FTSS_API.Service.Implement.Implement;
using FTSS_Model.Paginate;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller;

public class ProductController : BaseController<ProductController>
{
    private readonly IProductService _productService;
    public ProductController(ILogger<ProductController> logger, IProductService productService) : base(logger)
    {
        _productService = productService;
    }
    
    /// <summary>
    /// API tạo mới sản phẩm.
    /// </summary>
    /// <param name="createProductRequest">Thông tin sản phẩm cần tạo.</param>
    /// <returns>Trả về thông tin sản phẩm vừa tạo nếu thành công.</returns>
    [HttpPost(ApiEndPointConstant.Product.CreateNewProduct)]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequest createProductRequest, [FromServices] Supabase.Client client)
    {
        if (createProductRequest == null)
        {
            return BadRequest("Product request cannot be null.");
        }

        var response = await _productService.CreateProduct(createProductRequest, client);

        if (response.status == StatusCodes.Status201Created.ToString())
        {
            return CreatedAtAction(nameof(CreateProduct), new { id = response.data }, response);
        }
        else
        {
            return StatusCode(int.Parse(response.status), response);
        }
    }

    /// <summary>
    /// API upload hình ảnh mô tả cho sản phẩm.
    /// </summary>
    /// <param name="formFile">File ảnh được tải lên.</param>
    /// <returns>Trả về kết quả upload.</returns>
    [HttpPost(ApiEndPointConstant.Product.UploadImg)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> UploadImg(IFormFile formFile)
    {
        var response = await _productService.UpImageForDescription(formFile);
        return StatusCode(int.Parse(response.status), response);
    }

    /// <summary>
    /// API lấy danh sách sản phẩm dành cho Admin. Hiển thị tất cả các trạng thái sản phẩm.
    /// </summary>
    /// <returns>Danh sách sản phẩm phân trang.</returns>
    [HttpGet(ApiEndPointConstant.Product.GetListProducts)]
    [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetListProduct(
        [FromQuery] int? page = 1,
        [FromQuery] int? size = 10,
        [FromQuery] bool? isAscending = null,
        [FromQuery] string? subcategoryName = null,
        [FromQuery] string? productName = null,
        [FromQuery] string? cateName = null,
        [FromQuery] string? status = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null
    )
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;

        var response = await _productService.GetListProduct(
            pageNumber,
            pageSize,
            isAscending,
            subcategoryName,
            productName,
            cateName,
            status,
            minPrice,
            maxPrice
        );

        if (response == null || response.data == null)
        {
            return Problem(detail: MessageConstant.ProductMessage.ProductIsEmpty, statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(response);
    }

    /// <summary>
    /// API lấy danh sách sản phẩm dành cho người dùng (UI). Chỉ hiển thị các sản phẩm có trạng thái Available.
    /// </summary>
    /// <returns>Danh sách sản phẩm phân trang có trạng thái Available.</returns>
    [HttpGet(ApiEndPointConstant.Product.GetAllProducts)]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetAllProduct(
        [FromQuery] int? page = 1,
        [FromQuery] int? size = 10,
        [FromQuery] bool? isAscending = null,
        [FromQuery] string? subcategoryName = null,
        [FromQuery] string? productName = null,
        [FromQuery] string? cateName = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;

        var response = await _productService.GetAllProduct(
            pageNumber,
            pageSize,
            isAscending,
            subcategoryName,
            productName,
            cateName,
            minPrice,
            maxPrice);

        if (response == null || response.data == null)
        {
            return Problem(detail: MessageConstant.ProductMessage.ProductIsEmpty, statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(response);
    }

    /// <summary>
    /// API lấy danh sách sản phẩm theo danh mục.
    /// </summary>
    /// <param name="id">ID danh mục cần lấy sản phẩm.</param>
    /// <returns>Danh sách sản phẩm trong danh mục.</returns>
    [HttpGet(ApiEndPointConstant.Product.GetListProductsBySubCategory)]
    [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetListProductBySubCategoryId( [FromQuery] int? page, [FromQuery] int? size)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;

        var response = await _productService.GetAllProductsGroupedByCategory(pageNumber, pageSize);

        if (response == null)
        {
            return Problem(MessageConstant.ProductMessage.ProductIsEmpty);
        }

        return Ok(response);
    }

    /// <summary>
    /// API lấy thông tin chi tiết sản phẩm theo ID.
    /// </summary>
    /// <param name="id">ID sản phẩm cần lấy thông tin.</param>
    /// <returns>Thông tin sản phẩm.</returns>
    [HttpGet(ApiEndPointConstant.Product.GetProductById)]
    [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id)
    {
        var response = await _productService.GetProductById(id);

        if (response == null)
        {
            return Problem(MessageConstant.ProductMessage.ProductIsEmpty);
        }

        return Ok(response);
    }

    /// <summary>
    /// API cập nhật thông tin sản phẩm.
    /// </summary>
    /// <param name="id">ID sản phẩm cần cập nhật.</param>
    /// <param name="updateProductRequest">Thông tin sản phẩm cần cập nhật.</param>
    /// <returns>Kết quả cập nhật sản phẩm.</returns>
    [HttpPut(ApiEndPointConstant.Product.UpdateProduct)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromForm] UpdateProductRequest updateProductRequest, [FromServices] Supabase.Client client)
    {
        if (updateProductRequest == null)
        {
            return BadRequest("Product request cannot be null.");
        }

        var response = await _productService.UpdateProduct(id, updateProductRequest, client);

        if (response.status == StatusCodes.Status200OK.ToString())
        {
            return Ok(response);
        }

        return StatusCode(int.Parse(response.status), response);
    }

    /// <summary>
    /// API xóa sản phẩm.
    /// </summary>
    /// <param name="id">ID sản phẩm cần xóa.</param>
    /// <returns>Kết quả xóa sản phẩm.</returns>
    [HttpDelete(ApiEndPointConstant.Product.DeleteProduct)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id)
    {
        var response = await _productService.DeleteProduct(id);

        if (response == false)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API kích hoạt sản phẩm.
    /// </summary>
    /// <param name="id">ID sản phẩm cần kích hoạt.</param>
    /// <returns>Kết quả kích hoạt sản phẩm.</returns>
    [HttpPut(ApiEndPointConstant.Product.EnableProduct)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> EnableProduct([FromRoute] Guid id)
    {
        var response = await _productService.EnableProduct(id);

        if (response.status == StatusCodes.Status200OK.ToString())
        {
            return Ok(response);
        }

        return StatusCode(int.Parse(response.status), response);
    }
    // [HttpGet("proxy-image")]
    // [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    // [ProducesErrorResponseType(typeof(ProblemDetails))]
    // public async Task<IActionResult> ProxyImage([FromQuery] string url)
    // {
    //     try
    //     {
    //         using var httpClient = new HttpClient();
    //         httpClient.Timeout = TimeSpan.FromSeconds(30);
    //
    //         var request = new HttpRequestMessage(HttpMethod.Get, url);
    //         request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    //         request.Headers.Referrer = new Uri("https://thuysinhtim.vn");
    //
    //         var response = await httpClient.SendAsync(request);
    //
    //         if (!response.IsSuccessStatusCode)
    //         {
    //             _logger.LogWarning("Failed to fetch image from url {Url} - Status code: {StatusCode}", url, response.StatusCode);
    //             return Problem($"Cannot fetch image from url: {url}");
    //         }
    //
    //         var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    //         var imageBytes = await response.Content.ReadAsByteArrayAsync();
    //
    //         return File(imageBytes, contentType);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to fetch image from url {Url}", url);
    //         return Problem($"Cannot fetch image from url: {url}");
    //     }
    // }
    /// <summary>
    /// Gợi ý sản phẩm phù hợp cho bể cá dựa trên thông tin yêu cầu.
    /// </summary>
    /// <param name="request">Thông tin yêu cầu, bao gồm kích thước bể và các tiêu chí khác.</param>
    /// <returns>Trả về danh sách sản phẩm được gợi ý.</returns>
    /// <response code="200">Gợi ý sản phẩm thành công.</response>
    /// <response code="400">Yêu cầu không hợp lệ hoặc không tìm thấy sản phẩm phù hợp.</response>
    /// <response code="500">Lỗi hệ thống khi xử lý gợi ý sản phẩm.</response>
    [HttpPost("recommend-products")]
    public async Task<IActionResult> RecommendProducts([FromBody] TankRequest request)
    {
        var response = await _productService.RecommendProducts(request);
        return StatusCode(int.Parse(response.status), response);
    }
    
    /// <summary>
    /// API lấy danh sách sản phẩm bán chạy (dành cho người dùng). Chỉ hiển thị các sản phẩm có trạng thái Available.
    /// </summary>
    /// <returns>Danh sách sản phẩm phân trang, sắp xếp theo số lượng bán được.</returns>
    [HttpGet(ApiEndPointConstant.Product.GetTopSellingProducts)]
    [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] int? page = 1,
        [FromQuery] int? size = 10,
        [FromQuery] bool? isAscending = null,
        [FromQuery] string? subcategoryName = null,
        [FromQuery] string? productName = null,
        [FromQuery] string? cateName = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;

        var response = await _productService.GetTopSellingProducts(
            pageNumber,
            pageSize,
            isAscending,
            subcategoryName,
            productName,
            cateName,
            minPrice,
            maxPrice);

        if (response == null || response.data == null)
        {
            return Problem(detail: MessageConstant.ProductMessage.ProductIsEmpty, statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(response);
    }
}
