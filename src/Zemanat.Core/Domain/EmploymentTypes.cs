using Zemanat.Core.Text;

namespace Zemanat.Core.Domain;

/// <summary>Titles and parsing for <see cref="EmploymentType"/>.</summary>
public static class EmploymentTypes
{
    /// <summary>The Persian title shown in the UI and reports.</summary>
    public static string Title(this EmploymentType type) => type switch
    {
        EmploymentType.Official => "رسمی",
        EmploymentType.Probationary => "آزمایشی",
        EmploymentType.Contractual => "پیمانی",
        _ => string.Empty
    };

    /// <summary>
    /// Reads the title from the Excel file. Arabic letters and extra spaces are tolerated;
    /// any other text (for example the removed "قراردادی" type) is rejected.
    /// </summary>
    public static bool TryParse(string? text, out EmploymentType type)
    {
        var key = PersianText.CleanText(text).Replace(" ", string.Empty).Replace("‌", string.Empty);

        foreach (var candidate in Enum.GetValues<EmploymentType>())
        {
            if (candidate.Title().Replace(" ", string.Empty) == key)
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }
}
