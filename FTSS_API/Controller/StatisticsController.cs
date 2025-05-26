using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using FTSS_API.Payload.Response.Statistic;
using FTSS_API.Service.Interface;
using FTSS_API.Constant;

namespace FTSS_API.Controller
{
  
    public class StatisticsController : BaseController<StatisticsController>
    {
        private readonly ILogger<StatisticsController> _logger;
        private readonly IStatisticsService _statisticsService;


        public StatisticsController(ILogger<StatisticsController> logger, IStatisticsService statisticsService) : base(logger)
        {
            _logger = logger;
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// API lấy thống kê doanh thu, đơn hàng, sản phẩm bán ra và người dùng trong tháng hiện tại.
        /// </summary>
        [HttpGet("monthly")]
        [ProducesResponseType(typeof(MonthlyStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetMonthlyStatistics()
        {
            try
            {
                var response = await _statisticsService.GetMonthlyStatistics();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching monthly statistics: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }
        /// <summary>
        /// Lấy thống kê doanh số bán sản phẩm theo danh mục trong khoảng thời gian chỉ định.
        /// </summary>
        /// <param name="startDay">Ngày bắt đầu của khoảng thời gian (định dạng: yyyy-MM-dd).</param>
        /// <param name="endDay">Ngày kết thúc của khoảng thời gian (城镇: yyyy-MM-dd).</param>
        /// <returns>Trả về danh sách thống kê doanh số sản phẩm theo danh mục.</returns>
        /// <response code="200">Lấy thống kê doanh số theo danh mục thành công.</response>
        /// <response code="400">Khoảng thời gian không hợp lệ (ví dụ: startDay lớn hơn endDay).</response>
        /// <response code="500">Lỗi hệ thống khi truy xuất dữ liệu thống kê.</response>
        [HttpGet("category-sales")]
        public async Task<IActionResult> GetProductSalesByCategory([FromQuery] DateTime startDay, [FromQuery] DateTime endDay)
        {
            var result = await _statisticsService.GetProductSalesByCategory(startDay, endDay);
            return Ok(result);
        }
        /// <summary>
        /// Lấy thống kê doanh thu theo khoảng thời gian chỉ định.
        /// </summary>
        /// <param name="startDay">Ngày bắt đầu của khoảng thời gian (định dạng: yyyy-MM-dd).</param>
        /// <param name="endDay">Ngày kết thúc của khoảng thời gian (định dạng: yyyy-MM-dd).</param>
        /// <returns>Trả về thống kê doanh thu (MonthlyStatisticsResponse) trong khoảng thời gian.</returns>
        /// <response code="200">Lấy thống kê doanh thu thành công.</response>
        /// <response code="400">Khoảng thời gian không hợp lệ (ví dụ: startDay lớn hơn endDay).</response>
        /// <response code="500">Lỗi hệ thống khi truy xuất dữ liệu doanh thu.</response>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(MonthlyStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetRevenueByDateRange([FromQuery] DateTime startDay, [FromQuery] DateTime endDay)
        {
            var result = await _statisticsService.GetRevenueByDateRangeAsync(startDay, endDay);
            return Ok(result);
        }
        /// <summary>
        /// API lấy số lượng sản phẩm bán được trong tuần.
        /// </summary>
        [HttpGet("weekly-sales")]
        [ProducesResponseType(typeof(List<DailySalesResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetWeeklySales()
        {
            try
            {
                var response = await _statisticsService.GetWeeklySales();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching weekly sales: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }
        /// <summary>
        /// API lấy thống kê tài chính (doanh thu thực tế, tổng tiền hoàn trả, sản phẩm, dịch vụ).
        /// </summary>
        [HttpGet("financial-statistics")]
        [ProducesResponseType(typeof(FinancialStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetFinancialStatistics()
        {
            try
            {
                var response = await _statisticsService.GetFinancialStatistics();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching financial statistics: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}