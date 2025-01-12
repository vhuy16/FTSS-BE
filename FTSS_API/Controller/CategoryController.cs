using Microsoft.AspNetCore.Mvc;
using FTSS_API.Constant;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response;
using FTSS_API.Service.Interface;
using Microsoft.Extensions.Logging;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Category;

namespace FTSS_API.Controller
{
    [ApiController]
    [Route(ApiEndPointConstant.Category.CategoryEndPoint)]
    public class CategoryController : BaseController<CategoryController>
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ILogger<CategoryController> logger, ICategoryService categoryService) : base(logger) 
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// API tạo mới loại hàng.
        /// </summary>
        [HttpPost(ApiEndPointConstant.Category.CreateCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateCategory([FromForm] CategoryRequest createNewCategoryRequest, [FromServices] Supabase.Client client)
        {
            var response = await _categoryService.CreateCategory(createNewCategoryRequest, client);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy thông tin tất cả loại hàng.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Category.GetAllCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllCategory([FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? searchName = null,
                                                 [FromQuery] bool? isAscending = null)
        {
            var response = await _categoryService.GetAllCategory(page ?? 1, size ?? 10, searchName, isAscending);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy thông tin loại hàng.
        /// </summary>
        [HttpGet(ApiEndPointConstant.Category.GetCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetCategory([FromRoute] Guid id)
        {
            var response = await _categoryService.GetCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API cập nhật thông tin loại hàng.
        /// </summary>
        [HttpPut(ApiEndPointConstant.Category.UpdateCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCategory([FromRoute] Guid id, [FromForm] CategoryRequest updateCategoryRequest, [FromServices] Supabase.Client client)
        {
            var response = await _categoryService.UpdateCategory(id, updateCategoryRequest, client);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API xóa loại hàng.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Category.DeleteCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCategory([FromRoute] Guid id)
        {
            var response = await _categoryService.DeleteCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
