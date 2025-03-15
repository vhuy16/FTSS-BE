namespace FTSS_API.Payload.Response.Statistic;

public class MonthlyStatisticsResponse
{
    public StatisticItem Revenue { get; set; }
    public StatisticItem Orders { get; set; }
    public StatisticItem ProductsSold { get; set; }
    public StatisticItem Users { get; set; }
}

public class StatisticItem
{
    public decimal Value { get; set; }
    public string ChangePercentage { get; set; }
}

