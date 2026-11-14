using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Dates;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Core.Finance;
using Zemanat.Core.Settings;

namespace Zemanat.Data.Documents;

public sealed record ReportFilter(DateTime? FromIran = null, DateTime? ToIran = null, DocumentStatus? Status = null, DocumentType? Type = null);

public sealed record AuditRow(DateTime TimeUtc, string UserName, string Action, string Details);

/// <summary>The document report (screen and Excel) and the audit trail view.</summary>
public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Document>> QueryAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var q = _db.Documents.AsNoTracking().AsQueryable();
        // Dates are Iran calendar days; the clock in the database is UTC (Iran is UTC+3:30, no daylight saving).
        if (filter.FromIran is { } from) q = q.Where(d => d.CreatedUtc >= from.Date.AddHours(-3.5));
        if (filter.ToIran is { } to) q = q.Where(d => d.CreatedUtc < to.Date.AddDays(1).AddHours(-3.5));
        if (filter.Status is { } status) q = q.Where(d => d.Status == status);
        if (filter.Type is { } type) q = q.Where(d => d.Type == type);
        return await q.OrderBy(d => d.Id).Take(20000).ToListAsync(ct);
    }

    public static byte[] BuildExcel(IReadOnlyList<Document> documents, MoneyUnit unit)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("اسناد");
        sheet.RightToLeft = true;

        var headers = new[] { "شماره", "نوع", "وضعیت", "کد پرسنلی", "نام", "کد ملی", "واحد", $"مبلغ ({MoneyFormatter.UnitTitle(unit)})", "بانک / ذی‌نفع", "تاریخ ثبت", "تاریخ صدور" };
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var d in documents)
        {
            sheet.Cell(row, 1).Value = d.NumberText ?? string.Empty;
            sheet.Cell(row, 2).Value = d.Type.Title();
            sheet.Cell(row, 3).Value = d.Status.Title();
            sheet.Cell(row, 4).Value = d.PersonnelCode;
            sheet.Cell(row, 5).Value = d.PersonName;
            sheet.Cell(row, 6).Value = d.NationalCode;
            sheet.Cell(row, 7).Value = d.OrgUnit;
            sheet.Cell(row, 8).Value = MoneyFormatter.ToDisplayValue(d.AmountRial, unit);
            sheet.Cell(row, 9).Value = d.Beneficiary;
            sheet.Cell(row, 10).Value = JalaliDate.Format(IranClock.ToIran(d.CreatedUtc));
            sheet.Cell(row, 11).Value = d.IssuedUtc is { } issued ? JalaliDate.Format(IranClock.ToIran(issued)) : string.Empty;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<(IReadOnlyList<AuditRow> Items, int Total)> AuditPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var total = await _db.AuditLogs.CountAsync(ct);
        var items = await (from a in _db.AuditLogs.AsNoTracking()
                           join u in _db.Users.AsNoTracking() on a.UserId equals (int?)u.Id into users
                           from u in users.DefaultIfEmpty()
                           orderby a.Id descending
                           select new AuditRow(a.TimeUtc, u == null ? "-" : u.FirstName + " " + u.LastName, a.Action, a.Details))
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}
