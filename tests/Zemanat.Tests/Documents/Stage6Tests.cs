using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Core.Settings;
using Zemanat.Data;
using Zemanat.Data.Documents;
using Zemanat.Data.Entities;
using Zemanat.Data.Seeding;

namespace Zemanat.Tests.Documents;

public sealed class Stage6Tests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly int _personId;

    public Stage6Tests()
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

    private async Task<Document> IssueAsync(IServiceScope scope, long amount = 10_000_000)
    {
        var r = await scope.ServiceProvider.GetRequiredService<DocumentService>()
            .CreateAsync(1, false, _personId, DocumentType.Guarantee, amount, "بانک", null, false, new DocumentExtras("پدر", "آقای", "وام‌گیرنده", "پدر وام"));
        return r.Document!;
    }

    [Fact]
    public async Task Settle_and_cancel_are_final_and_free_the_ceiling()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var doc = await IssueAsync(scope, 60_000_000);

        Assert.False((await service.CancelAsync(doc.Id, 1, " ")).Success);
        Assert.True((await service.SettleAsync(doc.Id, 1)).Success);
        Assert.False((await service.SettleAsync(doc.Id, 1)).Success);
        Assert.False((await service.CancelAsync(doc.Id, 1, "اشتباه")).Success);
        Assert.Equal(70_000_000, (await service.PreviewAsync(_personId, 0))!.Remaining);

        var second = await IssueAsync(scope, 20_000_000);
        Assert.True((await service.CancelAsync(second.Id, 1, "اشتباه")).Success);
        Assert.Equal(DocumentStatus.Cancelled, (await service.GetAsync(second.Id))!.Status);
    }

    [Fact]
    public async Task Stats_count_by_status()
    {
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var a = await IssueAsync(scope);
        await IssueAsync(scope);
        await service.SettleAsync(a.Id, 1);

        var stats = await service.GetStatsAsync(_personId);
        Assert.Equal(1, stats.Issued);
        Assert.Equal(1, stats.Settled);
        Assert.Equal(10_000_000, stats.ActiveAmountRial);
        Assert.Equal(0, (await service.GetStatsAsync(99999)).Issued);
    }

    [Fact]
    public async Task Inquiry_is_numbered_once_per_document_and_renders()
    {
        using var scope = _provider.CreateScope();
        var inquiries = scope.ServiceProvider.GetRequiredService<InquiryService>();
        var doc = await IssueAsync(scope);

        Assert.False((await inquiries.CreateAsync(doc.Id, 1, "", "1405/08/23", null, null)).Success);
        Assert.False((await inquiries.CreateAsync(doc.Id, 1, "55", "not a date", null, null)).Success);

        var ok = await inquiries.CreateAsync(doc.Id, 1, "۵۵۶۶", "۱۴۰۵/۰۸/۲۳", null, null);
        Assert.True(ok.Success, ok.Error);
        Assert.Equal(2, ok.Inquiry!.Sequence); // the document took number 1 from the same counter
        Assert.Equal("وام‌گیرنده", ok.Inquiry.BorrowerName);
        Assert.False((await inquiries.CreateAsync(doc.Id, 1, "77", "1405/08/24", null, null)).Success);

        var html = await inquiries.RenderAsync(ok.Inquiry.Id);
        Assert.NotNull(html);
        Assert.DoesNotContain("{{", html);
        Assert.Contains("وام‌گیرنده", html);
    }

    [Fact]
    public async Task Inquiry_needs_an_issued_document()
    {
        using var scope = _provider.CreateScope();
        var inquiries = scope.ServiceProvider.GetRequiredService<InquiryService>();
        var pending = await scope.ServiceProvider.GetRequiredService<DocumentService>()
            .CreateAsync(1, false, _personId, DocumentType.Guarantee, 90_000_000, "بانک", null, true);
        Assert.False((await inquiries.CreateAsync(pending.Document!.Id, 1, "55", "1405/08/23", null, null)).Success);
    }

    [Fact]
    public void Validity_date_is_thirty_days_after_issue()
    {
        var doc = new Document { PersonName = "x", TrackingCode = "ABCDEFGHJKLM", NumberText = "1.1405.106", IssuedUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc) };
        var values = DocumentPrintService.BuildValues(doc, new SystemSettings(), "http://x/");
        Assert.Equal("۱۴۰۵/۰۱/۰۱", values["Date"]);
        Assert.Equal("۱۴۰۵/۰۱/۳۱", values["ValidUntil"]);
    }

    [Fact]
    public void Inquiry_template_uses_only_known_placeholders()
    {
        var html = TemplateService.DefaultHtml(TemplateKind.BankInquiry)!;
        var doc = new Document { PersonName = "x" };
        var inquiry = new BankInquiry { NumberText = "1.1405.106", BankLetterNumber = "1", BorrowerName = "y" };
        var known = InquiryService.BuildValues(inquiry, doc, new SystemSettings()).Keys.ToHashSet();
        foreach (var name in TemplateRenderer.PlaceholdersIn(html)) Assert.Contains(name, known);
    }
}
