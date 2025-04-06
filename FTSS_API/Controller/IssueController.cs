using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Response.Issue;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using FTSS_API.Payload.Request;

namespace FTSS_API.Controller
{
    [ApiController]
    [Route("api/issue")]
    public class IssueController : BaseController<IssueController>
    {
        private readonly IIssueService _issueService;

        public IssueController(ILogger<IssueController> logger, IIssueService issueService)
            : base(logger)
        {
            _issueService = issueService;
        }

        /// <summary>
        /// API tạo sự cố mới cùng với danh sách sản phẩm liên quan
        /// </summary>
        [HttpPost(ApiEndPointConstant.Issue.CreateIssue)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateIssue([FromBody] AddUpdateIssueRequest request)
        {
            var response = await _issueService.CreateIssue(request);
            if (response.status == StatusCodes.Status400BadRequest.ToString())
            {
                return BadRequest(response);
            }
            return CreatedAtAction(nameof(CreateIssue), response);
        }

        /// <summary>
        /// API lấy danh sách tất cả các sự cố
        /// </summary>
        [HttpGet(ApiEndPointConstant.Issue.GetAllIssues)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetAllIssues(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] bool? isAscending = null,
            [FromQuery] Guid? issueCategoryId = null)
        {
            var response = await _issueService.GetAllIssues(page, size, isAscending, issueCategoryId);
            return Ok(response);
        }

        /// <summary>
        /// API lấy thông tin sự cố theo ID
        /// </summary>
        [HttpGet(ApiEndPointConstant.Issue.GetIssueById)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetIssue([FromRoute] Guid id)
        {
            var response = await _issueService.GetIssueById(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API cập nhật thông tin sự cố
        /// </summary>
        [HttpPut(ApiEndPointConstant.Issue.UpdateIssue)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateIssue([FromRoute] Guid id, [FromBody] AddUpdateIssueRequest request)
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

            var response = await _issueService.UpdateIssue(id, request);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API xoá sự cố
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Issue.DeleteIssue)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteIssue([FromRoute] Guid id)
        {
            var response = await _issueService.DeleteIssue(id);
            return StatusCode(int.Parse(response.status), response);
        }
    }
}
