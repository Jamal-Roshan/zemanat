using System.Globalization;
using ClosedXML.Excel;
using Zemanat.Core.Personnel;
using Zemanat.Core.Text;

namespace Zemanat.Data.Personnel;

/// <summary>Reads the first worksheet of the personnel Excel file. Columns are found by their Persian header names.</summary>
public static class PersonnelExcelReader
{
    private static readonly string[] Headers =
        ["کد پرسنلی", "نام", "نام خانوادگی", "کد ملی", "خالص پرداختی", "نوع استخدام", "واحد سازمانی"];

    public static (IReadOnlyList<PersonnelRawRow> Rows, IReadOnlyList<PersonnelError> Errors) Read(Stream stream)
    {
        var rows = new List<PersonnelRawRow>();
        var errors = new List<PersonnelError>();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            errors.Add(new(0, "فایل قابل خواندن نیست. فقط فایل اکسل (xlsx) پذیرفته می‌شود."));
            return (rows, errors);
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null || sheet.LastRowUsed() is null)
            {
                errors.Add(new(0, "فایل اکسل خالی است."));
                return (rows, errors);
            }

            // Find each column by its normalized header text in the first row.
            var columns = new int[Headers.Length];
            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (var h = 0; h < Headers.Length; h++)
            {
                var wanted = Key(Headers[h]);
                for (var c = 1; c <= lastColumn; c++)
                {
                    if (Key(Text(sheet.Cell(1, c))) == wanted)
                    {
                        columns[h] = c;
                        break;
                    }
                }

                if (columns[h] == 0)
                {
                    errors.Add(new(0, $"ستون «{Headers[h]}» در سطر اول فایل پیدا نشد."));
                }
            }

            if (errors.Count > 0)
            {
                return (rows, errors);
            }

            var lastRow = sheet.LastRowUsed()!.RowNumber();
            for (var r = 2; r <= lastRow; r++)
            {
                var values = columns.Select(c => Text(sheet.Cell(r, c))).ToArray();
                if (values.All(string.IsNullOrWhiteSpace))
                {
                    continue; // skip blank rows
                }

                rows.Add(new PersonnelRawRow(r, values[0], values[1], values[2], values[3], values[4], values[5], values[6]));
            }

            if (rows.Count == 0)
            {
                errors.Add(new(0, "فایل هیچ ردیف داده‌ای ندارد."));
            }
        }

        return (rows, errors);
    }

    private static string Key(string? text) => PersianText.CleanText(text).Replace(" ", string.Empty).Replace("‌", string.Empty);

    private static string Text(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.Value.IsNumber
            ? cell.Value.GetNumber().ToString("0.################", CultureInfo.InvariantCulture)
            : cell.GetString();
    }
}
