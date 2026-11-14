using System.Reflection;
using Microsoft.EntityFrameworkCore;
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
}
