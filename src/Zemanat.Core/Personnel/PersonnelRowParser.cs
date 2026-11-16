using Zemanat.Core.Domain;
using Zemanat.Core.Text;

namespace Zemanat.Core.Personnel;

/// <summary>Checks and cleans every row of the personnel file. Nothing is saved unless there are no errors at all.</summary>
public static class PersonnelRowParser
{
    public static (IReadOnlyList<PersonnelRow> Rows, IReadOnlyList<PersonnelError> Errors) Parse(IEnumerable<PersonnelRawRow> raw)
    {
        var (rows, errors, skipped) = ParseLenient(raw);
        if (skipped.Count == 0) return (rows, errors);

        var all = errors.Concat(skipped.Select(x => new PersonnelError(x.RowNumber, "نوع استخدام باید رسمی، آزمایشی یا پیمانی باشد."))).OrderBy(e => e.RowNumber).ToList();
        return (rows, all);
    }

    /// <summary>
    /// Like <see cref="Parse"/>, but a row whose only problem is the employment type is not an error: it is skipped
    /// and reported in the third list (with its personnel code), and the other rows can still be imported.
    /// </summary>
    public static (IReadOnlyList<PersonnelRow> Rows, IReadOnlyList<PersonnelError> Errors, IReadOnlyList<SkippedPersonnel> Skipped) ParseLenient(IEnumerable<PersonnelRawRow> raw)
    {
        var rows = new List<PersonnelRow>();
        var errors = new List<PersonnelError>();
        var skipped = new List<SkippedPersonnel>();
        var seen = new Dictionary<string, int>();

        foreach (var r in raw)
        {
            var before = errors.Count;
            var code = PersianText.ToLatinDigits(PersianText.CleanText(r.Code)).Replace(" ", string.Empty);
            var first = PersianText.CleanText(r.FirstName);
            var last = PersianText.CleanText(r.LastName);
            var national = PersianText.ToLatinDigits(PersianText.CleanText(r.NationalCode)).Replace(" ", string.Empty);
            var unit = PersianText.CleanText(r.OrgUnit);

            if (code.Length == 0 || !code.All(char.IsAsciiDigit) || code.Length > 20)
            {
                errors.Add(new(r.RowNumber, "کد پرسنلی خالی یا نامعتبر است (فقط ارقام)."));
            }
            else if (seen.TryGetValue(code, out var firstRow))
            {
                errors.Add(new(r.RowNumber, $"کد پرسنلی تکراری است (ردیف {PersianText.ToPersianDigits(firstRow.ToString())} هم همین کد را دارد)."));
            }
            else
            {
                seen[code] = r.RowNumber;
            }

            if (first.Length == 0) errors.Add(new(r.RowNumber, "نام خالی است."));
            if (last.Length == 0) errors.Add(new(r.RowNumber, "نام خانوادگی خالی است."));

            if (national.Length != 10 || !national.All(char.IsAsciiDigit))
            {
                errors.Add(new(r.RowNumber, "کد ملی باید ۱۰ رقم باشد."));
            }

            long net = 0;
            if (!PersianText.TryParseAmount(r.NetPay, out net) || net <= 0)
            {
                errors.Add(new(r.RowNumber, "خالص پرداختی خالی یا نامعتبر است."));
            }

            var badType = !EmploymentTypes.TryParse(r.EmploymentType, out var type);

            if (errors.Count == before)
            {
                if (badType)
                {
                    skipped.Add(new(r.RowNumber, code, "نوع استخدام باید رسمی، آزمایشی یا پیمانی باشد؛ این ردیف وارد نشد."));
                }
                else
                {
                    rows.Add(new PersonnelRow(r.RowNumber, code, first, last, national, net, type, unit));
                }
            }
            else if (badType)
            {
                // The row is already invalid for other reasons; mention the type problem as well.
                errors.Add(new(r.RowNumber, "نوع استخدام باید رسمی، آزمایشی یا پیمانی باشد."));
            }
        }

        return (rows, errors, skipped);
    }
}

/// <summary>A row that was left out only because its employment type is not valid.</summary>
public sealed record SkippedPersonnel(int RowNumber, string Code, string Message);
