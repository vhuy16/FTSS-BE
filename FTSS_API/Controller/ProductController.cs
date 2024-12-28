using FTSS_API.Constant;
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
    
    [HttpPost(ApiEndPointConstant.Product.CreateNewProduct)]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequest createProductRequest)
    {
        if (createProductRequest == null)
        {
            return BadRequest("Product request cannot be null.");
        }

        var response = await _productService.CreateProduct(createProductRequest);

        if (response.status == StatusCodes.Status201Created.ToString())
        {
            return CreatedAtAction(nameof(CreateProduct), new { id = response.data }, response);
        }
        else
        {
            return StatusCode(int.Parse(response.status), response);
        }
    }

    [HttpPost(ApiEndPointConstant.Product.UploadImg)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> UploadImg(IFormFile formFile)
    {

        var response = await _productService.UpImageForDescription(formFile);

        return StatusCode(int.Parse(response.status), response);

    }
    [HttpGet(ApiEndPointConstant.Product.GetListProducts)]
    [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetListProduct(
        [FromQuery] int? page = 1,
        [FromQuery] int? size = 10,
        [FromQuery] bool? isAscending = null,
        [FromQuery] string? subcategoryName = null, // Lọc theo danh mục con
        [FromQuery] string? productName = null,    // Lọc theo tên sản phẩm
        [FromQuery] string? cateName = null,       // Lọc theo danh mục
        [FromQuery] string? status = null,         // Trạng thái sản phẩm
        [FromQuery] decimal? minPrice = null,      // Giá tối thiểu
        [FromQuery] decimal? maxPrice = null       // Giá tối đa
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


    [HttpGet(ApiEndPointConstant.Product.GetAllProducts)]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetAllProduct( [FromQuery] int? page = 1,
        [FromQuery] int? size = 10,
        [FromQuery] bool? isAscending = null,
        [FromQuery] string? subcategoryName = null, // Lọc theo danh mục con
        [FromQuery] string? productName = null,    // Lọc theo tên sản phẩm
        [FromQuery] string? cateName = null,       // Lọc theo danh m// Trạng thái sản phẩm
        [FromQuery] decimal? minPrice = null,      // Giá tối thiểu
        [FromQuery] decimal? maxPrice = null   )
    {

        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;
        
        var response = await _productService.GetAllProduct( pageNumber,
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

    [HttpGet(ApiEndPointConstant.Product.GetListProductsByCategoryId)]
    [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetListProductByCategoryId([FromRoute] Guid id, [FromQuery] int? page, [FromQuery] int? size)
    {
        int pageNumber = page ?? 1;
        int pageSize = size ?? 10;
        var response = await _productService.GetListProductByCategoryId(id, pageNumber, pageSize);
        if (response == null)
        {
            return Problem(MessageConstant.ProductMessage.ProductIsEmpty);
        }
        return Ok(response);
    }
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
    
    [HttpPut(ApiEndPointConstant.Product.UpdateProduct)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromForm] UpdateProductRequest updateProductRequest)
    {
        if (updateProductRequest == null)
        {
            return BadRequest("Product request cannot be null.");
        }

        var response = await _productService.UpdateProduct(id, updateProductRequest);

        if (response.status == StatusCodes.Status200OK.ToString())
        {
            return Ok(response);
        }

        return StatusCode(int.Parse(response.status), response);
    }
    
    [HttpDelete(ApiEndPointConstant.Product.UpdateProduct)]
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
}
