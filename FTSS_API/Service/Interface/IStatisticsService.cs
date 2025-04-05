using FTSS_API.Payload.Response.Statistic;

namespace FTSS_API.Service.Interface;

public interface IStatisticsService
{
    Task<MonthlyStatisticsResponse> GetMonthlyStatistics();
    Task<List<RevenueResponse>> GetRevenueByDateRangeAsync(DateTime startDay, DateTime endDay);
    Task<List<DailySalesResponse>> GetWeeklySales();
    Task<List<CategorySalesResponse>> GetProductSalesByCategory(DateTime startDay, DateTime endDay);
}