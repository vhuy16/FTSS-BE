using FTSS_API.Payload.Response.Statistic;

namespace FTSS_API.Service.Interface;

public interface IStatisticsService
{
    Task<MonthlyStatisticsResponse> GetMonthlyStatistics();
}