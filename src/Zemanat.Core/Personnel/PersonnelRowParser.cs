using Zemanat.Core.Domain;
using Zemanat.Core.Text;

namespace Zemanat.Core.Personnel;

/// <summary>Checks and cleans every row of the personnel file. Nothing is saved unless there are no errors at all.</summary>
public static class PersonnelRowParser
{
    public static (IReadOnlyList<PersonnelRow> Rows, IReadOnlyList<PersonnelError> Errors) Parse(IEnumerable<PersonnelRawRow> raw)
    {
        var rows = new List<PersonnelRow>();
        var errors = new List<PersonnelError>();
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

            if (!EmploymentTypes.TryParse(r.EmploymentType, out var type))
            {
                errors.Add(new(r.RowNumber, "نوع استخدام باید رسمی، آزمایشی یا پیمانی باشد."));
            }

            if (errors.Count == before)
            {
                rows.Add(new PersonnelRow(r.RowNumber, code, first, last, national, net, type, unit));
            }
        }

        return (rows, errors);
    }
}
