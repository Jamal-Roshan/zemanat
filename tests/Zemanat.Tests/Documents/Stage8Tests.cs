using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Domain;
using Zemanat.Core.Finance;
using Zemanat.Core.Settings;
using Zemanat.Data;
using Zemanat.Data.Documents;
using Zemanat.Data.Entities;
using Zemanat.Data.Seeding;

namespace Zemanat.Tests.Documents;

public sealed class Stage8Tests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly int _personId;

    public Stage8Tests()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZemanatData(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync().GetAwaiter().GetResult();
        var p = new AppUser { UserName = "3001", FirstName = "نام", LastName = "ج", NationalCode = "0000000003", NetPayRial = 100_000_000, IsActive = true };
        db.Users.Add(p);
        db.SaveChanges();
        _personId = p.Id;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Actions_are_written_to_the_audit_log_and_report_filters_work()
    {
        using var scope = _provider.CreateScope();
        var docs = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var reports = scope.ServiceProvider.GetRequiredService<ReportService>();

        var created = await docs.CreateAsync(1, false, _personId, DocumentType.Guarantee, 10_000_000, "بانک", null, false);
        await docs.SettleAsync(created.Document!.Id, 1);

        var (items, total) = await reports.AuditPageAsync(1, 10);
        Assert.Equal(2, total);
        Assert.Equal(new[] { "تسویه", "صدور" }, items.Select(i => i.Details.Split(' ')[0]).ToArray());

        var all = await reports.QueryAsync(new ReportFilter());
        Assert.Single(all);
        Assert.Empty(await reports.QueryAsync(new ReportFilter(Status: DocumentStatus.Issued)));
        Assert.Single(await reports.QueryAsync(new ReportFilter(Status: DocumentStatus.Settled, Type: DocumentType.Guarantee)));
        Assert.Empty(await reports.QueryAsync(new ReportFilter(FromIran: DateTime.UtcNow.AddDays(2))));
    }

    [Fact]
    public async Task Excel_export_has_a_header_and_one_row_per_document()
    {
        using var scope = _provider.CreateScope();
        var docs = scope.ServiceProvider.GetRequiredService<DocumentService>();
        await docs.CreateAsync(1, false, _personId, DocumentType.Guarantee, 10_000_000, "بانک ملی", null, false);
        var rows = await scope.ServiceProvider.GetRequiredService<ReportService>().QueryAsync(new ReportFilter());

        var bytes = ReportService.BuildExcel(rows, MoneyUnit.Toman);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.Equal(2, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal(1_000_000, sheet.Cell(2, 8).GetDouble());
        Assert.Equal("بانک ملی", sheet.Cell(2, 9).GetString());
    }
}
