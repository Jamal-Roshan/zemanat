using System.Globalization;
using Zemanat.Core.Text;

namespace Zemanat.Core.Dates;

/// <summary>Jalali (Persian) calendar helpers. Dates are stored as Gregorian in the database and shown as Jalali.</summary>
public static class JalaliDate
{
    private static readonly PersianCalendar Calendar = new();

    /// <summary>Names of the Jalali months, index 0 is Farvardin.</summary>
    public static readonly IReadOnlyList<string> MonthNames =
    [
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    ];

    /// <summary>The Jalali year of a Gregorian date.</summary>
    public static int Year(DateTime date) => Calendar.GetYear(date);

    /// <summary>Formats a date as yyyy/MM/dd in the Jalali calendar. Returns an empty string for dates the calendar cannot represent.</summary>
    public static string Format(DateTime date, bool persianDigits = false)
    {
        if (date.Year < 623)
        {
            return string.Empty;
        }

        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"{Calendar.GetYear(date):0000}/{Calendar.GetMonth(date):00}/{Calendar.GetDayOfMonth(date):00}");
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    /// <summary>Formats a date and time as "yyyy/MM/dd HH:mm".</summary>
    public static string FormatWithTime(DateTime date, bool persianDigits = false)
    {
        var datePart = Format(date, persianDigits: false);
        if (datePart.Length == 0)
        {
            return string.Empty;
        }

        var text = datePart + " " + date.ToString("HH:mm", CultureInfo.InvariantCulture);
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    /// <summary>Converts a Jalali date to Gregorian. Throws if the date does not exist.</summary>
    public static DateTime ToGregorian(int year, int month, int day) =>
        Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);

    /// <summary>Parses text such as 1404/7/1 (any digit script, separators "/", "-" or ".").</summary>
    public static bool TryParse(string? text, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = PersianText.ToLatinDigits(text).Trim().Replace('-', '/').Replace('.', '/');
        var parts = normalized.Split('/');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
        {
            return false;
        }

        try
        {
            if (month < 1 || month > 12 || day < 1 || day > Calendar.GetDaysInMonth(year, month))
            {
                return false;
            }

            result = Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
