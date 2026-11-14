using System.Net;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Zemanat.Core.Dates;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Core.Finance;
using Zemanat.Core.Settings;
using Zemanat.Core.Text;
using Zemanat.Data.Settings;

namespace Zemanat.Data.Documents;

/// <summary>Builds the printable page of an issued document and finds documents by their public tracking code.</summary>
public class DocumentPrintService
{
    private readonly AppDbContext _db;
    private readonly SettingsStore _settings;
    private readonly TemplateService _templates;

    public DocumentPrintService(AppDbContext db, SettingsStore settings, TemplateService templates)
    {
        _db = db;
        _settings = settings;
        _templates = templates;
    }

    /// <summary>Returns the finished HTML, or null if the document is missing, not issued yet, or not the caller's.</summary>
    public async Task<string?> RenderAsync(int documentId, int userId, bool isStaff, string fallbackBaseUrl, CancellationToken ct = default)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null) return null;
        if (!isStaff && doc.PersonId != userId) return null;
        if (doc.Status is not (DocumentStatus.Issued or DocumentStatus.Settled)) return null;

        var settings = await _settings.LoadAsync(ct);
        var kind = doc.Type == DocumentType.Guarantee ? TemplateKind.Guarantee : TemplateKind.SalaryCertificate;
        var template = await _templates.GetHtmlAsync(kind, ct);
        if (template is null) return null;

        return TemplateRenderer.Render(template, BuildValues(doc, settings, fallbackBaseUrl));
    }

    /// <summary>The public authenticity lookup. The code is the only key.</summary>
    public async Task<Document?> FindByTrackingCodeAsync(string? code, CancellationToken ct = default)
    {
        var normalized = TrackingCode.Normalize(code);
        if (!TrackingCode.IsWellFormed(normalized)) return null;
        return await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.TrackingCode == normalized, ct);
    }

    public static string VerifyUrl(SystemSettings settings, string fallbackBaseUrl, string trackingCode)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings.WebsiteBaseUrl) ? fallbackBaseUrl : settings.WebsiteBaseUrl.Trim();
        return baseUrl.TrimEnd('/') + "/v/" + trackingCode;
    }

    public static IReadOnlyDictionary<string, string> BuildValues(Document doc, SystemSettings s, string fallbackBaseUrl)
    {
        var issued = IranClock.ToIran(doc.IssuedUtc ?? doc.CreatedUtc);
        var validUntil = JalaliDate.Format(issued.AddDays(s.ValidityDays), persianDigits: true);

        return new Dictionary<string, string>
        {
            ["Title"] = doc.Type == DocumentType.Guarantee ? s.GuaranteeTitle : s.SalaryCertificateTitle,
            ["OrgName"] = s.OrganizationName,
            ["OrgAddress"] = s.OrganizationAddress,
            ["IssuingUnit"] = s.IssuingUnitTitle,
            ["SignerName"] = s.SignerName,
            ["SignerPosition"] = s.SignerPosition,
            ["Date"] = JalaliDate.Format(issued, persianDigits: true),
            ["Number"] = PersianText.ToPersianDigits(doc.NumberText ?? string.Empty),
            ["FullName"] = doc.PersonName,
            ["FatherName"] = doc.FatherName ?? string.Empty,
            ["NationalCode"] = PersianText.ToPersianDigits(doc.NationalCode),
            ["PersonnelCode"] = PersianText.ToPersianDigits(doc.PersonnelCode),
            ["EmploymentType"] = doc.EmploymentType.Title(),
            ["GenderPrefix"] = doc.GenderPrefix,
            ["WorkPlace"] = doc.OrgUnit,
            ["Bank"] = doc.Beneficiary,
            ["Amount"] = MoneyFormatter.Format(doc.AmountRial, MoneyUnit.Rial),
            ["AmountWords"] = MoneyFormatter.AmountInWords(doc.AmountRial, MoneyUnit.Rial),
            ["NetPay"] = MoneyFormatter.Format(doc.NetPayRial, MoneyUnit.Rial),
            ["NetPayWords"] = MoneyFormatter.AmountInWords(doc.NetPayRial, MoneyUnit.Rial),
            ["LoanOwnerName"] = doc.LoanOwnerName ?? string.Empty,
            ["LoanOwnerFatherName"] = doc.LoanOwnerFatherName ?? string.Empty,
            ["ValidUntil"] = validUntil,
            ["QR"] = QrImageHtml(VerifyUrl(s, fallbackBaseUrl, doc.TrackingCode)),
            ["Sign"] = SignatureHtml(s.SignatureImageDataUri)
        };
    }

    /// <summary>A PNG QR code as an inline image (works offline and when printed).</summary>
    public static string QrImageHtml(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(8);
        return $"<img class=\"pt-qr\" alt=\"QR\" src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" />";
    }

    /// <summary>Only PNG or JPEG data URIs are accepted, so nothing else can be injected into the page.</summary>
    public static string SignatureHtml(string? dataUri)
    {
        if (string.IsNullOrWhiteSpace(dataUri)) return string.Empty;
        var ok = dataUri.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
                 || dataUri.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal);
        if (!ok) return string.Empty;
        return $"<img class=\"pt-sign\" alt=\"\" src=\"{WebUtility.HtmlEncode(dataUri)}\" />";
    }
}
