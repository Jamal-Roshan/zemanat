using System.Globalization;
using Zemanat.Core.Text;

namespace Zemanat.Core.Finance;

/// <summary>
/// Converts between the stored amount (always Rials) and the unit chosen in the admin settings (Rial or Toman).
/// </summary>
public static class MoneyFormatter
{
    private const char ThousandsSeparator = '٬';
    private const char DecimalSeparator = '٫';

    /// <summary>The Persian title of the unit ("ریال" or "تومان").</summary>
    public static string UnitTitle(MoneyUnit unit) => unit == MoneyUnit.Toman ? "تومان" : "ریال";

    /// <summary>The amount expressed in the chosen unit.</summary>
    public static decimal ToDisplayValue(long rial, MoneyUnit unit) =>
        unit == MoneyUnit.Toman ? rial / 10m : rial;

    /// <summary>Converts a whole amount typed in the chosen unit to Rials.</summary>
    public static long ToRial(long amountInUnit, MoneyUnit unit) =>
        unit == MoneyUnit.Toman ? checked(amountInUnit * 10) : amountInUnit;

    /// <summary>Formats an amount with thousands separators, e.g. "۱٬۵۰۰٬۰۰۰".</summary>
    public static string Format(long rial, MoneyUnit unit, bool persianDigits = true)
    {
        var value = ToDisplayValue(rial, unit);
        var text = value.ToString("#,##0.##", CultureInfo.InvariantCulture)
            .Replace(',', ThousandsSeparator)
            .Replace('.', DecimalSeparator);
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    /// <summary>Formats an amount followed by its unit, e.g. "۱٬۵۰۰٬۰۰۰ ریال".</summary>
    public static string FormatWithUnit(long rial, MoneyUnit unit, bool persianDigits = true) =>
        Format(rial, unit, persianDigits) + " " + UnitTitle(unit);

    /// <summary>
    /// The amount in Persian words followed by the unit. Amounts are entered in whole units,
    /// so for Toman any remainder below one Toman (less than 10 Rials) is not spelled out.
    /// </summary>
    public static string AmountInWords(long rial, MoneyUnit unit)
    {
        var whole = unit == MoneyUnit.Toman ? rial / 10 : rial;
        return NumberWords.ToWords(whole) + " " + UnitTitle(unit);
    }
}
