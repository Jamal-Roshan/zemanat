using Microsoft.AspNetCore.Authorization;
using Zemanat.Core.Dates;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Data.Documents;
using Zemanat.Data.Settings;

namespace Zemanat.Web.Auth;

/// <summary>The Excel download of the document report (a file response cannot come from a Blazor page).</summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/reports/documents.xlsx", async (string? from, string? to, int? status, int? type, ReportService reports, SettingsStore store) =>
        {
            var filter = BuildFilter(from, to, status, type);
            var rows = await reports.QueryAsync(filter);
            var settings = await store.LoadAsync();
            var bytes = ReportService.BuildExcel(rows, settings.MoneyUnit);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "documents.xlsx");
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Manager,Operator" });
    }

    public static ReportFilter BuildFilter(string? from, string? to, int? status, int? type) => new(
        JalaliDate.TryParse(from, out var f) ? f : null,
        JalaliDate.TryParse(to, out var t) ? t : null,
        status is { } s && Enum.IsDefined(typeof(DocumentStatus), s) ? (DocumentStatus)s : null,
        type is { } k && Enum.IsDefined(typeof(DocumentType), k) ? (DocumentType)k : null);
}
