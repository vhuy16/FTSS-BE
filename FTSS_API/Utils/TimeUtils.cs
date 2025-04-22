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
        // Lấy thời gian UTC và chuyển sang SEA (UTC+7)
        DateTime utcTime = DateTime.UtcNow; // Luôn là UTC, DateTimeKind.Utc
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, SeaTimeZone); // Trả về SEA, DateTimeKind.Unspecified
    }

    public static DateTime GetCurrentSEATimeAsUtc()
    {
        return DateTime.UtcNow; // Dùng để lưu vào cơ sở dữ liệu
    }

    public static DateTime ConvertToSEATime(DateTime utcValue)
    {
        if (utcValue.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Đầu vào DateTime phải là UTC.", nameof(utcValue));
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, SeaTimeZone); // Chuyển UTC sang SEA
    }
}