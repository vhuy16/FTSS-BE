using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.IssueCategory;
using FTSS_API.Payload.Response.IssueCategory;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FTSS_API.Controller
{
    [ApiController]
    [Route("api/issue-category")]
    public class IssueCategoryController : BaseController<IssueCategoryController>
    {
        private readonly IIssueCategoryService _issueCategoryService;

        public IssueCategoryController(ILogger<IssueCategoryController> logger, IIssueCategoryService issueCategoryService)
            : base(logger)
        {
            _issueCategoryService = issueCategoryService;
        }

        /// <summary>
        /// API tạo danh mục sự cố mới
        /// </summary>
        [HttpPost(ApiEndPointConstant.IssueCategory.CreateIssueCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateIssueCategory([FromBody] AddUpdateIssueCategoryRequest request)
        {
            var response = await _issueCategoryService.CreateIssueCategory(request);
            if (response.status == StatusCodes.Status400BadRequest.ToString())
            {
                return BadRequest(response);
            }
            return CreatedAtAction(nameof(CreateIssueCategory), response);
        }

        /// <summary>
        /// API lấy danh sách tất cả danh mục sự cố
        /// </summary>
        [HttpGet(ApiEndPointConstant.IssueCategory.GetAllIssueCategories)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllIssueCategories()
        {
            var response = await _issueCategoryService.GetAllIssueCategories();
            return Ok(response);
        }

        /// <summary>
        /// API lấy danh mục sự cố theo ID
        /// </summary>
        [HttpGet(ApiEndPointConstant.IssueCategory.GetIssueCategoryById)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetIssueCategory([FromRoute] Guid id)
        {
            var response = await _issueCategoryService.GetIssueCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API cập nhật danh mục sự cố
        /// </summary>
        [HttpPut(ApiEndPointConstant.IssueCategory.UpdateIssueCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateIssueCategory([FromRoute] Guid id, [FromBody] AddUpdateIssueCategoryRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    data = null,
                    message = "Invalid request data",
                    status = StatusCodes.Status400BadRequest.ToString(),
                });
            }

            var response = await _issueCategoryService.UpdateIssueCategory(id, request);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API xoá danh mục sự cố
        /// </summary>
        [HttpDelete(ApiEndPointConstant.IssueCategory.DeleteIssueCategory)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteIssueCategory([FromRoute] Guid id)
        {
            var response = await _issueCategoryService.DeleteIssueCategory(id);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
