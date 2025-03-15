﻿using Microsoft.AspNetCore.Mvc;
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
    }
}