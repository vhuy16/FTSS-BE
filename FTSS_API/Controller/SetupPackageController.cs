﻿using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.SetupPackage;
using FTSS_API.Payload.Request.SubCategory;
using FTSS_API.Payload.Response;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Paginate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Supabase;

namespace FTSS_API.Controller
{

    public class SetupPackageController : BaseController<SubCategoryController>
    {
        private readonly ISetupPackageService _setupPackageService;
        public SetupPackageController(ILogger<SubCategoryController> logger, ISetupPackageService setupPackageService) : base(logger)
        {
            _setupPackageService = setupPackageService;
        }
        /// <summary>
        /// API tạo mới setup cho manager, customer.
        /// </summary>
        [HttpPost(ApiEndPointConstant.SetupPackage.AddSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> AddSetupPackage([FromForm] AddSetupPackageRequest request, [FromServices] Supabase.Client client)
        {
            List<ProductSetupItem> productids;
            try
            {
                productids = JsonConvert.DeserializeObject<List<ProductSetupItem>>(request.ProductItemsJson);
                if (productids == null || productids.Count == 0)
                {
                    return BadRequest(new ApiResponse { status = "400", message = "Danh sách sản phẩm không được để trống" });
                }
            }
            catch (JsonException)
            {
                return BadRequest(new ApiResponse { status = "400", message = "Định dạng danh sách sản phẩm không hợp lệ" });
            }

            var response = await _setupPackageService.AddSetupPackage(productids, request, client);

            // 🔹 Tránh lỗi vòng lặp bằng cách sử dụng PreserveReferencesHandling
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API delete setup cho manager, customer.
        /// </summary>
        [HttpDelete(ApiEndPointConstant.SetupPackage.RemoveSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> RemoveSetupPackage([FromRoute] Guid id)
        {
            var response = await _setupPackageService.RemoveSetupPackage(id);
            return StatusCode(int.Parse(response.status), response);
        }

        /// <summary>
        /// API lấy danh sách SetupPackage của customer cho customer.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetListSetupPackage)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListSetupPackage(
            [FromQuery] int? page = 1,
            [FromQuery] int? size = 10,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _setupPackageService.GetListSetupPackage(pageNumber, pageSize, isAscending);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.SetUpPackageMessage.SetUpPackageIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }

        /// <summary>
        /// API lấy danh sách SetupPackage của shop cho mọi role.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetListSetupPackageAllShop)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListSetupPackageAllShop(
            [FromQuery] int? page = 1,
            [FromQuery] int? size = 10,
            [FromQuery] bool? isAscending = null,
            [FromQuery] double? minPrice = null,
            [FromQuery] double? maxPrice = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _setupPackageService.GetListSetupPackageAllShop(pageNumber, pageSize, isAscending, minPrice, maxPrice);
            if (response == null || response.data == null)
            {
                return Problem(detail: MessageConstant.SetUpPackageMessage.SetUpPackageIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }

        /// <summary>
        /// API lấy danh sách SetupPackage của user cho manager.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetListSetupPackageAllUser)]
        [ProducesResponseType(typeof(IPaginate<ApiResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetListSetupPackageAllUser(
            [FromQuery] int? page = 1,
            [FromQuery] int? size = 10,
            [FromQuery] bool? isAscending = null)
        {
            int pageNumber = page ?? 1;
            int pageSize = size ?? 10;
            var response = await _setupPackageService.GetListSetupPackageAllUser(pageNumber, pageSize, isAscending);
            if (response == null && response.data == null)
            {
                return Problem(detail: MessageConstant.SetUpPackageMessage.SetUpPackageIsEmpty,
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        /// <summary>
        /// API cập nhật thông tin SetupPackage cho manager, customer.
        /// </summary>
        [HttpPut(ApiEndPointConstant.SetupPackage.UpdateSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateSetupPackage(
            [FromRoute] Guid setupPackageId,
            [FromForm] UpdateSetupPackageRequest request,
            [FromServices] Supabase.Client client)
        {
            List<ProductSetupItem> productIds = null;
    
            if (!string.IsNullOrEmpty(request.ProductItemsJson))
            {
                try
                {
                    productIds = JsonConvert.DeserializeObject<List<ProductSetupItem>>(request.ProductItemsJson);
                    if (productIds != null && productIds.Count == 0)
                    {
                        return BadRequest(new ApiResponse { status = "400", message = "Danh sách sản phẩm không hợp lệ" });
                    }
                }
                catch (JsonException)
                {
                    return BadRequest(new ApiResponse { status = "400", message = "Định dạng danh sách sản phẩm không hợp lệ" });
                }
            }

            var response = await _setupPackageService.UpdateSetupPackage(setupPackageId, productIds, request, client);
            return StatusCode(int.Parse(response.status), response);
        }
        /// <summary>
        /// API sao chép SetupPackage.
        /// </summary>
        [HttpPost(ApiEndPointConstant.SetupPackage.CopySetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CopySetupPackage([FromRoute] Guid setupPackageId)
        {
            var response = await _setupPackageService.CopySetupPackage(setupPackageId);
            return StatusCode(int.Parse(response.status), response);
        }
            
        /// <summary>
        /// API lấy thông tin chi tiết setup theo ID cho mọi role.
        /// </summary>
        [HttpGet(ApiEndPointConstant.SetupPackage.GetSetUpById)]
        [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetProductById([FromRoute] Guid id)
        {
            var response = await _setupPackageService.GetSetUpById(id);

            if (response == null)
            {
                return Problem(MessageConstant.SetUpPackageMessage.SetUpPackageIsEmpty, statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(response);
        }
        
        /// <summary>
        /// API kích hoạt SetupPackage cho customer.
        /// </summary>
        [HttpPut(ApiEndPointConstant.SetupPackage.EnableSetupPackage)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> enableSetupPackage(
            [FromRoute] Guid setupPackageId 
           )
        {
            var response = await _setupPackageService.enableSetupPackage(setupPackageId);
            return Ok(response);
        }
    }
}
