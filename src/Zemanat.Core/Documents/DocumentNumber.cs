using System.Globalization;
using Zemanat.Core.Text;

namespace Zemanat.Core.Documents;

/// <summary>
/// The secretariat number of a document: "sequence.year.code", for example "123.1405.106".
/// The sequence restarts from 1 every Jalali year and is shared by guarantees, salary certificates and bank inquiries.
/// The code and the separator come from the admin settings.
/// </summary>
public readonly record struct DocumentNumber(int Sequence, int Year, string Code)
{
    public const string DefaultSeparator = ".";

    /// <summary>Formats the number, e.g. "123.1405.106".</summary>
    public string Format(string separator = DefaultSeparator, bool persianDigits = false)
    {
        var text = string.Create(CultureInfo.InvariantCulture, $"{Sequence}{separator}{Year}{separator}{Code}");
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    /// <summary>Parses text produced by <see cref="Format"/> (any digit script).</summary>
    public static bool TryParse(string? text, string separator, out DocumentNumber number)
    {
        number = default;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(separator))
        {
            return false;
        }

        var parts = PersianText.ToLatinDigits(text).Trim().Split(separator, 3);
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || sequence < 1
            || year < 1
            || string.IsNullOrWhiteSpace(parts[2]))
        {
            return false;
        }

        number = new DocumentNumber(sequence, year, parts[2].Trim());
        return true;
    }
}
