using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;

namespace Zemanat.Data.Documents;

/// <summary>Print templates: an edited copy from the database, or the built-in default taken from the old .mrt reports.</summary>
public class TemplateService
{
    private readonly AppDbContext _db;

    public TemplateService(AppDbContext db) => _db = db;

    public static string? DefaultHtml(TemplateKind kind)
    {
        var name = kind switch
        {
            TemplateKind.Guarantee => "Templates.Guarantee.html",
            TemplateKind.SalaryCertificate => "Templates.SalaryCertificate.html",
            TemplateKind.BankInquiry => "Templates.BankInquiry.html",
            _ => null
        };
        if (name is null) return null;

        using var stream = typeof(TemplateService).Assembly.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task<string?> GetHtmlAsync(TemplateKind kind, CancellationToken ct = default)
    {
        var edited = await _db.PrintTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Kind == kind, ct);
        return edited?.Html ?? DefaultHtml(kind);
    }

    public async Task SaveAsync(TemplateKind kind, string html, CancellationToken ct = default)
    {
        var row = await _db.PrintTemplates.FirstOrDefaultAsync(t => t.Kind == kind, ct);
        if (row is null) _db.PrintTemplates.Add(row = new PrintTemplate { Kind = kind });
        row.Html = html;
        row.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Goes back to the built-in default by removing the edited copy.</summary>
    public async Task ResetAsync(TemplateKind kind, CancellationToken ct = default)
    {
        var row = await _db.PrintTemplates.FirstOrDefaultAsync(t => t.Kind == kind, ct);
        if (row is null) return;
        _db.PrintTemplates.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Placeholder names a template of this kind may use (the keys that printing supplies).</summary>
    public static IReadOnlySet<string> KnownKeys(TemplateKind kind)
    {
        var settings = new Zemanat.Core.Settings.SystemSettings();
        var doc = new Document { PersonName = "x" };
        return kind == TemplateKind.BankInquiry
            ? InquiryService.BuildValues(new BankInquiry { NumberText = "1", BankLetterNumber = "1", BorrowerName = "x" }, doc, settings).Keys.ToHashSet()
            : DocumentPrintService.BuildValues(doc, settings, "http://localhost/").Keys.ToHashSet();
    }

    /// <summary>Placeholders in the text that printing does not know. They would print as empty.</summary>
    public static IReadOnlyList<string> UnknownPlaceholders(TemplateKind kind, string html)
    {
        var known = KnownKeys(kind);
        return TemplateRenderer.PlaceholdersIn(html).Where(n => !known.Contains(n)).Distinct().ToList();
    }

    /// <summary>Renders the template with made-up data so the manager can check the layout.</summary>
    public static string RenderSample(TemplateKind kind, string html)
    {
        var settings = new Zemanat.Core.Settings.SystemSettings();
        var doc = new Document
        {
            PersonName = "نام نمونه", PersonnelCode = "1000", NationalCode = "0000000000", Beneficiary = "بانک نمونه",
            AmountRial = 100_000_000, NumberText = "1.1405.106", TrackingCode = "ABCDEFGHJKLM", IssuedUtc = DateTime.UtcNow
        };
        var values = kind == TemplateKind.BankInquiry
            ? InquiryService.BuildValues(new BankInquiry { NumberText = "2.1405.106", BankLetterNumber = "123", BorrowerName = "نام نمونه", IssuedUtc = DateTime.UtcNow }, doc, settings)
            : DocumentPrintService.BuildValues(doc, settings, "http://localhost/");
        return TemplateRenderer.Render(html, values);
    }
}
