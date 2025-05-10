namespace FTSS_API.Payload.Response.Statistic;

public class FinancialStatisticItem
{
    public string Name { get; set; }
    public decimal Value { get; set; }
}

public class FinancialStatisticsResponse
{
    public List<FinancialStatisticItem> Statistics { get; set; }
}