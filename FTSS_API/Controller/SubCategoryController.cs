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
        [HttpPost(ApiEndPointConstant.SubCategory.CreateSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateSubCategory([FromBody] SubCategoryRequest createSubCategoryRequest)
        {
            var response = await _subCategoryService.CreateSubCategory(createSubCategoryRequest);
            return StatusCode(int.Parse(response.status), response);
        }

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

        [HttpGet(ApiEndPointConstant.SubCategory.GetSubCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetSubCategory([FromRoute] Guid id)
        {
            var response = await _subCategoryService.GetSubCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }

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
