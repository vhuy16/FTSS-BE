using AutoMapper;
using FTSS_API.Payload.Response.Statistic;using FTSS_API.Service.Implement;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

public class StatisticsService : BaseService<StatisticsService>, IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;


    public StatisticsService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<StatisticsService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MonthlyStatisticsResponse> GetMonthlyStatistics()
    {
        var currentMonth = DateTime.UtcNow.Month;
        var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var currentYear = DateTime.UtcNow.Year;
        var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var currentMonthOrders = await _unitOfWork.GetRepository<Order>().GetListAsync(
            include: i => i.Include(i => i.OrderDetails),
            predicate: o => o.CreateDate.Value.Month == currentMonth && o.CreateDate.Value.Year == currentYear && o.Status.Equals(OrderStatus.COMPLETED.GetDescriptionFromEnum()));
        var previousMonthOrders = await _unitOfWork.GetRepository<Order>().GetListAsync(
            include: i => i.Include(i => i.OrderDetails),
            predicate: o => o.CreateDate.Value.Month == previousMonth && o.CreateDate.Value.Year == previousYear&& o.Status.Equals(OrderStatus.COMPLETED.GetDescriptionFromEnum()));

        decimal currentRevenue = currentMonthOrders.Sum(o => o.TotalPrice);
        decimal previousRevenue = previousMonthOrders.Sum(o => o.TotalPrice);

        int currentOrders = currentMonthOrders.Count;
        int previousOrders = previousMonthOrders.Count;

        int currentProductsSold = currentMonthOrders
            .Where(o => o.OrderDetails != null && o.OrderDetails.Any()) // Chỉ lấy đơn hàng có OrderDetails
            .SelectMany(o => o.OrderDetails)
            .Sum(od => od.Quantity);

        int previousProductsSold = previousMonthOrders
            .Where(o => o.OrderDetails != null && o.OrderDetails.Any())
            .SelectMany(o => o.OrderDetails)
            .Sum(od => od.Quantity);

        int currentUsers = currentMonthOrders.Select(o => o.UserId).Distinct().Count();
        int previousUsers = previousMonthOrders.Select(o => o.UserId).Distinct().Count();

        return new MonthlyStatisticsResponse
        {
            Revenue = new StatisticItem
            {
                Value = currentRevenue,
                ChangePercentage = CalculateChangePercentage(currentRevenue, previousRevenue)
            },
            Orders = new StatisticItem
            {
                Value = currentOrders,
                ChangePercentage = CalculateChangePercentage(currentOrders, previousOrders)
            },
            ProductsSold = new StatisticItem
            {
                Value = currentProductsSold,
                ChangePercentage = CalculateChangePercentage(currentProductsSold, previousProductsSold)
            },
            Users = new StatisticItem
            {
                Value = currentUsers,
                ChangePercentage = CalculateChangePercentage(currentUsers, previousUsers)
            }
        };
    }

    public async Task<List<RevenueResponse>> GetRevenueByDateRangeAsync(DateTime startDay, DateTime endDay)
    {
        var revenues = new List<RevenueResponse>();

        for (var date = startDay; date <= endDay; date = date.AddDays(1))
        {
            var dailyOrders = await _unitOfWork.GetRepository<Order>().GetListAsync(
                predicate: o => o.CreateDate.Value.Date == date.Date && o.Status == OrderStatus.COMPLETED.GetDescriptionFromEnum());

            decimal totalRevenue = dailyOrders.Sum(o => o.TotalPrice);

            revenues.Add(new RevenueResponse
            {
                Day = date.ToString("dd/MM/yyyy"),
                Revenue = totalRevenue
            });
        }

        return revenues;
    }
    private string CalculateChangePercentage(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? "100%" : "0%";
        decimal change = ((current - previous) / previous) * 100;
        return change >= 0 ? $"{change:F2}%" : $"-{Math.Abs(change):F2}%";
    }
    public async Task<List<DailySalesResponse>> GetWeeklySales()
    {
        DateTime today = DateTime.UtcNow;

        // Xác định ngày đầu tuần (Thứ Hai)
        int currentDayOfWeek = (int)today.DayOfWeek;
        DateTime startOfWeek = today.AddDays(-((currentDayOfWeek == 0 ? 7 : currentDayOfWeek) - 1)).Date;
        DateTime endOfWeek = startOfWeek.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        var weeklyOrders = await _unitOfWork.GetRepository<Order>().GetListAsync(
            include: i => i.Include(o => o.OrderDetails),
            predicate: o => o.CreateDate >= startOfWeek && o.CreateDate <= endOfWeek
                         && o.Status.Equals(OrderStatus.COMPLETED.GetDescriptionFromEnum()));

        // Dữ liệu bán hàng theo ngày thực tế (nếu có)
        var salesData = weeklyOrders
            .SelectMany(o => o.OrderDetails, (order, detail) => new { order.CreateDate, detail.Quantity })
            .GroupBy(d => d.CreateDate.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // Tạo danh sách kết quả gồm tất cả các ngày trong tuần
        List<DailySalesResponse> result = new List<DailySalesResponse>();

        for (DateTime date = startOfWeek; date <= endOfWeek; date = date.AddDays(1))
        {
            result.Add(new DailySalesResponse
            {
                Day = date.ToString("dd/MM/yyyy"),
                ProductQuantity = salesData.ContainsKey(date) ? salesData[date] : 0
            });
        }

        return result;
    }
    public async Task<List<CategorySalesResponse>> GetProductSalesByCategory(DateTime startDay, DateTime endDay)
    {
        var orders = await _unitOfWork.GetRepository<Order>().GetListAsync(
            include: o => o.Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.SubCategory)
                .ThenInclude(p => p.Category),
            predicate: o => o.CreateDate >= startDay && o.CreateDate <= endDay 
                                                     && o.Status.Equals(OrderStatus.COMPLETED.GetDescriptionFromEnum()));

        var salesData = orders
            .SelectMany(o => o.OrderDetails)
            .GroupBy(od => od.Product.SubCategory.Category.CategoryName) // Giả sử mỗi sản phẩm có một Category
            .Select(g => new CategorySalesResponse
            {
                Category = g.Key,
                ProductQuantity = g.Sum(od => od.Quantity)
            })
            .ToList();

        return salesData;
    }

}
