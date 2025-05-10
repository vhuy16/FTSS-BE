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
        [HttpGet("category-sales")]
        public async Task<IActionResult> GetProductSalesByCategory([FromQuery] DateTime startDay, [FromQuery] DateTime endDay)
        {
            var result = await _statisticsService.GetProductSalesByCategory(startDay, endDay);
            return Ok(result);
        }

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