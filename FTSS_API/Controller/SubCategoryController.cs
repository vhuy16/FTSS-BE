using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SubCategory;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller
{
    [ApiController]
    [Route(ApiEndPointConstant.SubCategory.SubCategoryEndPoint)]
    public class SubCategoryController : BaseController<SubCategoryController>
    {
        private readonly ISubCategoryService _subCategoryService;
        public SubCategoryController(ILogger<SubCategoryController> logger, ISubCategoryService subCategoryService) : base(logger)
        {
            _subCategoryService = subCategoryService;
        }

        /// <summary>
        /// API tạo mới subcategory.
        /// </summary>
        [HttpPost(ApiEndPointConstant.SubCategory.CreateSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateSubCategory([FromBody] SubCategoryRequest createSubCategoryRequest)
        {
            var response = await _subCategoryService.CreateSubCategory(createSubCategoryRequest);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// Lấy danh sách tất cả danh mục phụ (subcategories) với phân trang và tùy chọn tìm kiếm, sắp xếp.
        /// </summary>
        /// <param name="page">Số trang của danh sách danh mục phụ (mặc định: 1).</param>
        /// <param name="size">Số lượng danh mục phụ mỗi trang (mặc định: 10).</param>
        /// <param name="searchName">Tên danh mục phụ để tìm kiếm (tùy chọn).</param>
        /// <param name="isAscending">Sắp xếp theo thứ tự tăng dần (true) hoặc giảm dần (false) (tùy chọn).</param>
        /// <returns>Trả về danh sách danh mục phụ với thông tin phân trang.</returns>
        /// <response code="200">Lấy danh sách danh mục phụ thành công.</response>
        /// <response code="404">Không tìm thấy danh mục phụ nào phù hợp.</response>
        /// <response code="500">Lỗi hệ thống khi truy xuất danh sách danh mục phụ.</response>
        [HttpGet(ApiEndPointConstant.SubCategory.GetAllSubCategories)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllSubCategories([FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? searchName = null,
                                                              [FromQuery] bool? isAscending = null)
        {
            var response = await _subCategoryService.GetAllSubCategories(page ?? 1, size ?? 10, searchName, isAscending);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// Lấy thông tin chi tiết của một danh mục phụ theo ID.
        /// </summary>
        /// <param name="id">ID của danh mục phụ cần truy xuất.</param>
        /// <returns>Trả về thông tin chi tiết của danh mục phụ.</returns>
        /// <response code="200">Lấy thông tin danh mục phụ thành công.</response>
        /// <response code="404">Không tìm thấy danh mục phụ với ID cung cấp.</response>
        /// <response code="500">Lỗi hệ thống khi truy xuất thông tin danh mục phụ.</response>
        [HttpGet(ApiEndPointConstant.SubCategory.GetSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetSubCategory([FromRoute] Guid id)
        {
            var response = await _subCategoryService.GetSubCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API cập nhật subcategory.
        /// </summary>
        [HttpPut(ApiEndPointConstant.SubCategory.UpdateSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateSubCategory([FromRoute] Guid id, [FromBody] SubCategoryRequest updateSubCategoryRequest)
        {
            var response = await _subCategoryService.UpdateSubCategory(id, updateSubCategoryRequest);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API enable subcategory.
        /// </summary>
        [HttpPut(ApiEndPointConstant.SubCategory.EnableSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> EnableSubCategory([FromRoute] Guid id)
        {
            var response = await _subCategoryService.EnableSubCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API xóa subcategory.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.SubCategory.DeleteSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteSubCategory([FromRoute] Guid id)
        {
            var response = await _subCategoryService.DeleteSubCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
