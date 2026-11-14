using System.Data;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Core.Finance;
using Zemanat.Core.Security;
using Zemanat.Core.Settings;
using Zemanat.Core.Text;
using Zemanat.Data.Entities;
using Zemanat.Data.Settings;

namespace Zemanat.Data.Documents;

public sealed record DocumentResult(string? Error, Document? Document = null)
{
    public bool Success => Error is null;
}

public sealed record DocumentExtras(string? FatherName, string? GenderPrefix, string? LoanOwnerName, string? LoanOwnerFatherName);

public sealed record DocumentFilter(string? Text = null, DocumentStatus? Status = null, DocumentType? Type = null, int? PersonId = null);

public sealed record DocumentPage(IReadOnlyList<Document> Items, int Total);

/// <summary>Creating, approving and listing documents. The ceiling check and numbering run inside one serializable transaction.</summary>
public class DocumentService
{
    private readonly AppDbContext _db;
    private readonly SettingsStore _settingsStore;

    public DocumentService(AppDbContext db, SettingsStore settingsStore)
    {
        _db = db;
        _settingsStore = settingsStore;
    }

    /// <summary>The ceiling position of one person for a possible new amount (0 shows only cap and remaining).</summary>
    public async Task<CreditAssessment?> PreviewAsync(int personId, long requestedRial, CancellationToken ct = default)
    {
        var person = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == personId, ct);
        if (person is null) return null;

        var settings = await _settingsStore.LoadAsync(ct);
        var active = await ActiveTotalAsync(personId, ct);
        var cap = CreditCalculator.CalculateCap(person.NetPayRial, settings.CreditPercent, settings.CreditMonths);
        if (requestedRial <= 0)
        {
            return new CreditAssessment(cap, active, Math.Max(0, cap - active), 0, CreditClass.Normal, 0);
        }

        return CreditCalculator.Assess(person.NetPayRial, settings.CreditPercent, settings.CreditMonths, active, requestedRial);
    }

    /// <param name="creatorId">The signed-in user.</param>
    /// <param name="creatorIsManager">A manager's documents are issued directly, even surplus ones.</param>
    public async Task<DocumentResult> CreateAsync(int creatorId, bool creatorIsManager, int personId, DocumentType type,
        long amountRial, string? beneficiary, string? description, bool acceptSurplus, DocumentExtras? extras = null, CancellationToken ct = default)
    {
        if (amountRial <= 0) return new("مبلغ باید بیشتر از صفر باشد.");
        var bank = PersianText.CleanText(beneficiary);
        if (bank.Length == 0) return new("نام بانک یا مرجع درخواست‌کننده را بنویسید.");
        if (!Enum.IsDefined(type)) return new("نوع سند نامعتبر است.");

        var settings = await _settingsStore.LoadAsync(ct);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var person = await _db.Users.FirstOrDefaultAsync(u => u.Id == personId, ct);
        if (person is null || !person.IsActive) return new("فرد انتخاب‌شده پیدا نشد یا غیرفعال است.");
        if (person.NetPayRial <= 0) return new("خالص پرداختی این فرد ثبت نشده است.");

        var active = await ActiveTotalAsync(personId, ct);
        var assessment = CreditCalculator.Assess(person.NetPayRial, settings.CreditPercent, settings.CreditMonths, active, amountRial);

        if (assessment.IsSurplus)
        {
            if (!settings.AllowSurplus) return new($"مبلغ از سقف مجاز باقی‌مانده بیشتر است (حداکثر {MoneyFormatter.FormatWithUnit(assessment.Remaining, settings.MoneyUnit)}).");
            if (!acceptSurplus) return new("برای مبلغ مازاد باید تعهدنامه را بپذیرید.");
        }

        var doc = new Document
        {
            PersonId = person.Id,
            Type = type,
            AmountRial = amountRial,
            Beneficiary = bank,
            Description = string.IsNullOrWhiteSpace(description) ? null : PersianText.CleanText(description),
            IsSurplus = assessment.IsSurplus,
            ExcessAmountRial = assessment.ExcessAmount,
            SurplusCommitmentAccepted = assessment.IsSurplus && acceptSurplus,
            PersonnelCode = person.UserName ?? string.Empty,
            PersonName = person.FullName,
            NationalCode = person.NationalCode,
            OrgUnit = person.OrgUnit,
            EmploymentType = person.EmploymentType,
            NetPayRial = person.NetPayRial,
            CapRial = assessment.Cap,
            FatherName = Clean(extras?.FatherName),
            GenderPrefix = extras?.GenderPrefix == "خانم" ? "خانم" : "آقای",
            LoanOwnerName = Clean(extras?.LoanOwnerName),
            LoanOwnerFatherName = Clean(extras?.LoanOwnerFatherName),
            TrackingCode = TrackingCode.Generate(),
            CreatedByUserId = creatorId,
            CreatedUtc = DateTime.UtcNow
        };

        var waits = !creatorIsManager && DocumentRules.NeedsApproval(settings.ApprovalMode, assessment.IsSurplus);
        _db.Documents.Add(doc);
        if (waits)
        {
            doc.Status = DocumentStatus.PendingApproval;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            await IssueAsync(doc, settings, ct);
            if (creatorIsManager && assessment.IsSurplus)
            {
                doc.DecidedByUserId = creatorId;
                doc.DecidedUtc = doc.IssuedUtc;
            }

            await _db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return new(null, doc);
    }

    public async Task<DocumentResult> ApproveAsync(int documentId, int approverId, string? note, CancellationToken ct = default)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null) return new("سند پیدا نشد.");
        if (doc.Status != DocumentStatus.PendingApproval) return new("این سند در انتظار تأیید نیست.");

        await IssueAsync(doc, settings, ct);
        doc.DecidedByUserId = approverId;
        doc.DecidedUtc = doc.IssuedUtc;
        doc.DecisionNote = string.IsNullOrWhiteSpace(note) ? null : PersianText.CleanText(note);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(null, doc);
    }

    public async Task<DocumentResult> RejectAsync(int documentId, int approverId, string? note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note)) return new("دلیل رد درخواست را بنویسید.");

        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null) return new("سند پیدا نشد.");
        if (doc.Status != DocumentStatus.PendingApproval) return new("این سند در انتظار تأیید نیست.");

        doc.Status = DocumentStatus.Rejected;
        doc.DecidedByUserId = approverId;
        doc.DecidedUtc = DateTime.UtcNow;
        doc.DecisionNote = PersianText.CleanText(note);
        await _db.SaveChangesAsync(ct);
        return new(null, doc);
    }

    public async Task<DocumentPage> SearchAsync(DocumentFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Documents.AsNoTracking().AsQueryable();
        if (filter.PersonId is not null) q = q.Where(d => d.PersonId == filter.PersonId);
        if (filter.Status is not null) q = q.Where(d => d.Status == filter.Status);
        if (filter.Type is not null) q = q.Where(d => d.Type == filter.Type);

        var text = PersianText.ToLatinDigits(PersianText.CleanText(filter.Text));
        if (text.Length > 0)
        {
            q = q.Where(d => d.PersonnelCode.Contains(text) || d.PersonName.Contains(text) || d.NationalCode.Contains(text)
                             || d.Beneficiary.Contains(text) || (d.NumberText != null && d.NumberText.Contains(text)));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(d => d.Id).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items, total);
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : PersianText.CleanText(text);

    private async Task IssueAsync(Document doc, SystemSettings settings, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var number = await DocumentNumbering.NextAsync(_db, settings, now, ct);
        doc.Sequence = number.Sequence;
        doc.SequenceYear = number.Year;
        doc.NumberText = number.Format(settings.DocumentNumberSeparator);
        doc.Status = DocumentStatus.Issued;
        doc.IssuedUtc = now;
    }

    private async Task<long> ActiveTotalAsync(int personId, CancellationToken ct) =>
        await _db.Documents
            .Where(d => d.PersonId == personId && (d.Status == DocumentStatus.PendingApproval || d.Status == DocumentStatus.Issued))
            .SumAsync(d => (long?)d.AmountRial, ct) ?? 0L;
}
