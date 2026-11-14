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
using Zemanat.Data.Settings;

namespace Zemanat.Tests.Documents;

public class TemplateRendererTests
{
    [Fact]
    public void Values_are_html_encoded_but_raw_keys_are_not()
    {
        var html = TemplateRenderer.Render("<b>{{FullName}}</b>{{QR}}{{Missing}}", new Dictionary<string, string>
        {
            ["FullName"] = "<script>x</script>",
            ["QR"] = "<img src=\"a\" />"
        });
        Assert.Equal("<b>&lt;script&gt;x&lt;/script&gt;</b><img src=\"a\" />", html);
    }

    [Theory]
    [InlineData(TemplateKind.Guarantee)]
    [InlineData(TemplateKind.SalaryCertificate)]
    public void Built_in_templates_use_only_known_placeholders(TemplateKind kind)
    {
        var html = TemplateService.DefaultHtml(kind);
        Assert.NotNull(html);
        var doc = new Document { PersonName = "x", TrackingCode = "ABCDEFGHJKLM", NumberText = "1.1405.106", IssuedUtc = DateTime.UtcNow };
        var known = DocumentPrintService.BuildValues(doc, new SystemSettings(), "https://example.test/").Keys.ToHashSet();
        foreach (var name in TemplateRenderer.PlaceholdersIn(html!))
        {
            Assert.Contains(name, known);
        }
    }

    [Fact]
    public void Signature_only_accepts_png_and_jpeg_data_uris()
    {
        Assert.Equal(string.Empty, DocumentPrintService.SignatureHtml("javascript:alert(1)"));
        Assert.Equal(string.Empty, DocumentPrintService.SignatureHtml("data:text/html;base64,AAAA"));
        Assert.Contains("<img", DocumentPrintService.SignatureHtml("data:image/png;base64,AAAA"));
    }

    [Fact]
    public void Qr_points_to_the_verify_page()
    {
        var s = new SystemSettings { WebsiteBaseUrl = "https://zemanat.example.ir/" };
        Assert.Equal("https://zemanat.example.ir/v/ABC", DocumentPrintService.VerifyUrl(s, "http://fallback/", "ABC"));
        Assert.Equal("http://fallback/v/ABC", DocumentPrintService.VerifyUrl(new SystemSettings(), "http://fallback/", "ABC"));
        Assert.Contains("data:image/png;base64,", DocumentPrintService.QrImageHtml("https://x.test/v/ABC"));
    }
}

public sealed class PrintServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly int _personId, _otherId;

    public PrintServiceTests()
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
        var a = new AppUser { UserName = "2001", FirstName = "نام", LastName = "الف", NationalCode = "0000000001", NetPayRial = 100_000_000, IsActive = true };
        var b = new AppUser { UserName = "2002", FirstName = "نام", LastName = "ب", NationalCode = "0000000002", NetPayRial = 100_000_000, IsActive = true };
        db.Users.AddRange(a, b);
        db.SaveChanges();
        _personId = a.Id;
        _otherId = b.Id;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Issued_document_renders_for_owner_and_staff_but_not_for_others()
    {
        using var scope = _provider.CreateScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var printer = scope.ServiceProvider.GetRequiredService<DocumentPrintService>();

        var created = await documents.CreateAsync(1, false, _personId, DocumentType.Guarantee, 10_000_000, "بانک نمونه", null, false,
            new DocumentExtras("پدر", "خانم", "وام‌گیرنده", "پدر وام‌گیرنده"));
        var id = created.Document!.Id;

        var html = await printer.RenderAsync(id, _personId, false, "http://localhost/");
        Assert.NotNull(html);
        Assert.Contains("بانک نمونه", html);
        Assert.Contains("وام‌گیرنده", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.DoesNotContain("{{", html);

        Assert.NotNull(await printer.RenderAsync(id, _otherId, true, "http://localhost/"));
        Assert.Null(await printer.RenderAsync(id, _otherId, false, "http://localhost/"));
    }

    [Fact]
    public async Task Pending_document_cannot_be_printed()
    {
        using var scope = _provider.CreateScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var printer = scope.ServiceProvider.GetRequiredService<DocumentPrintService>();
        var pending = await documents.CreateAsync(1, false, _personId, DocumentType.SalaryCertificate, 90_000_000, "بانک", null, true);
        Assert.Equal(DocumentStatus.PendingApproval, pending.Document!.Status);
        Assert.Null(await printer.RenderAsync(pending.Document.Id, _personId, true, "http://localhost/"));
    }

    [Fact]
    public async Task Tracking_code_lookup_is_forgiving_but_strict()
    {
        using var scope = _provider.CreateScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var printer = scope.ServiceProvider.GetRequiredService<DocumentPrintService>();
        var created = await documents.CreateAsync(1, false, _personId, DocumentType.SalaryCertificate, 10_000_000, "بانک", null, false);
        var code = created.Document!.TrackingCode;

        Assert.NotNull(await printer.FindByTrackingCodeAsync(code));
        Assert.NotNull(await printer.FindByTrackingCodeAsync(code.ToLowerInvariant()));
        Assert.Null(await printer.FindByTrackingCodeAsync("AAAAAAAAAAAA"));
        Assert.Null(await printer.FindByTrackingCodeAsync("short"));
        Assert.Null(await printer.FindByTrackingCodeAsync(null));
    }

    [Fact]
    public async Task Edited_template_overrides_default_and_reset_restores_it()
    {
        using var scope = _provider.CreateScope();
        var templates = scope.ServiceProvider.GetRequiredService<TemplateService>();
        await templates.SaveAsync(TemplateKind.Guarantee, "<p>{{FullName}}</p>");
        Assert.Equal("<p>{{FullName}}</p>", await templates.GetHtmlAsync(TemplateKind.Guarantee));
        await templates.ResetAsync(TemplateKind.Guarantee);
        Assert.Equal(TemplateService.DefaultHtml(TemplateKind.Guarantee), await templates.GetHtmlAsync(TemplateKind.Guarantee));
    }
}
