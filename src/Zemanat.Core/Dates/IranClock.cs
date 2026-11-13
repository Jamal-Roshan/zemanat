namespace Zemanat.Core.Dates;

/// <summary>
/// Converts between UTC (what the database stores) and Iran local time (what users see).
/// Falls back to a fixed +03:30 offset when the server has no time-zone database.
/// </summary>
public static class IranClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    /// <summary>Converts a UTC date-time to Iran time.</summary>
    public static DateTime ToIran(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>The current date and time in Iran.</summary>
    public static DateTime NowIran() => ToIran(DateTime.UtcNow);

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Iran", TimeSpan.FromHours(3.5), "Iran", "Iran");
    }
}
