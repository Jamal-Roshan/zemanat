using System.Data;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Dates;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Core.Finance;
using Zemanat.Core.Settings;
using Zemanat.Core.Text;
using Zemanat.Data.Settings;

namespace Zemanat.Data.Documents;

public sealed record InquiryResult(string? Error, BankInquiry? Inquiry = null)
{
    public bool Success => Error is null;
}

/// <summary>Bank inquiries: one per document, issued by an operator, with a number from the shared secretariat counter.</summary>
public class InquiryService
{
    private readonly AppDbContext _db;
    private readonly SettingsStore _settings;
    private readonly TemplateService _templates;

    public InquiryService(AppDbContext db, SettingsStore settings, TemplateService templates)
    {
        _db = db;
        _settings = settings;
        _templates = templates;
    }

    public Task<BankInquiry?> GetByDocumentAsync(int documentId, CancellationToken ct = default) =>
        _db.BankInquiries.AsNoTracking().FirstOrDefaultAsync(i => i.DocumentId == documentId, ct);

    public async Task<InquiryResult> CreateAsync(int documentId, int userId, string? bankLetterNumber, string? bankLetterDate,
        string? borrowerName, string? borrowerFatherName, CancellationToken ct = default)
    {
        var letterNumber = PersianText.ToLatinDigits(PersianText.CleanText(bankLetterNumber));
        if (letterNumber.Length == 0) return new("شماره‌ی نامه‌ی بانک را بنویسید.");
        if (!JalaliDate.TryParse(bankLetterDate, out var letterDate)) return new("تاریخ نامه‌ی بانک را به صورت شمسی (مثل ۱۴۰۵/۰۸/۲۳) بنویسید.");

        var settings = await _settings.LoadAsync(ct);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null) return new("سند پیدا نشد.");
        if (doc.Status is not (DocumentStatus.Issued or DocumentStatus.Settled)) return new("استعلام فقط برای سند صادرشده یا تسویه‌شده ثبت می‌شود.");
        if (await _db.BankInquiries.AnyAsync(i => i.DocumentId == documentId, ct)) return new("برای این سند قبلاً استعلام ثبت شده است.");

        var now = DateTime.UtcNow;
        var number = await DocumentNumbering.NextAsync(_db, settings, now, ct);
        var inquiry = new BankInquiry
        {
            DocumentId = documentId,
            BankLetterNumber = letterNumber,
            BankLetterDate = letterDate.Date,
            BorrowerName = Pick(borrowerName, doc.LoanOwnerName, doc.PersonName)!,
            BorrowerFatherName = Pick(borrowerFatherName, doc.LoanOwnerFatherName, doc.FatherName),
            Sequence = number.Sequence,
            SequenceYear = number.Year,
            NumberText = number.Format(settings.DocumentNumberSeparator),
            IssuedUtc = now,
            CreatedByUserId = userId
        };
        _db.BankInquiries.Add(inquiry);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(null, inquiry);
    }

    public async Task<string?> RenderAsync(int inquiryId, CancellationToken ct = default)
    {
        var inquiry = await _db.BankInquiries.AsNoTracking().Include(i => i.Document).FirstOrDefaultAsync(i => i.Id == inquiryId, ct);
        if (inquiry?.Document is null) return null;

        var template = await _templates.GetHtmlAsync(TemplateKind.BankInquiry, ct);
        if (template is null) return null;
        return TemplateRenderer.Render(template, BuildValues(inquiry, inquiry.Document, await _settings.LoadAsync(ct)));
    }

    public static IReadOnlyDictionary<string, string> BuildValues(BankInquiry i, Document d, SystemSettings s) => new Dictionary<string, string>
    {
        ["Bank"] = d.Beneficiary,
        ["BankLetterNumber"] = PersianText.ToPersianDigits(i.BankLetterNumber),
        ["BankLetterDate"] = JalaliDate.Format(i.BankLetterDate, persianDigits: true),
        ["Number"] = PersianText.ToPersianDigits(d.NumberText ?? string.Empty),
        ["Date"] = JalaliDate.Format(IranClock.ToIran(d.IssuedUtc ?? d.CreatedUtc), persianDigits: true),
        ["BorrowerName"] = i.BorrowerName,
        ["BorrowerFatherName"] = i.BorrowerFatherName ?? string.Empty,
        ["Amount"] = MoneyFormatter.Format(d.AmountRial, MoneyUnit.Rial),
        ["FullName"] = d.PersonName,
        ["EmploymentType"] = d.EmploymentType.Title(),
        ["SignerName"] = s.SignerName,
        ["SignerPosition"] = s.SignerPosition,
        ["InquiryNumber"] = PersianText.ToPersianDigits(i.NumberText),
        ["InquiryDate"] = JalaliDate.Format(IranClock.ToIran(i.IssuedUtc), persianDigits: true),
        ["Sign"] = DocumentPrintService.SignatureHtml(s.SignatureImageDataUri)
    };

    private static string? Pick(params string?[] options) =>
        options.Select(o => string.IsNullOrWhiteSpace(o) ? null : PersianText.CleanText(o)).FirstOrDefault(o => o is not null);
}
