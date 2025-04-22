namespace FTSS_API.Utils;

public static class TimeUtils
{
    private static readonly TimeZoneInfo SeaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Hoặc "Asia/Ho_Chi_Minh" cho Linux

    public static string GetTimestamp(DateTime value)
    {
        return value.ToString("yyyyMMddHHmmssff");
    }

    public static string GetHoursTime(DateTime value)
    {
        return value.ToString("H:mm");
    }

    public static DateTime GetCurrentSEATime()
    {
        DateTime utcTime = DateTime.UtcNow;
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, SeaTimeZone);
    }

    public static DateTime GetCurrentSEATimeAsUtc()
    {
        return DateTime.UtcNow; // Lưu UTC
    }

    public static DateTime ConvertToSEATime(DateTime value)
    {
        // Nếu đầu vào không phải UTC, giả định nó là UTC (dựa trên dữ liệu từ Supabase)
        DateTime utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, SeaTimeZone); // Chuyển UTC sang SEA
    }
}