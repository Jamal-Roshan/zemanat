using Zemanat.Core.Domain;
using Zemanat.Core.Finance;

namespace Zemanat.Core.Settings;

/// <summary>
/// Every setting the manager can change. Each public property is stored as one row (key = property name) in the Settings table.
/// The values below are the defaults used when nothing has been saved yet.
/// </summary>
public sealed class SystemSettings
{
    // Organization
    public string OrganizationName { get; set; } = "مدیریت آموزش و پرورش ملایر";
    public string IssuingUnitTitle { get; set; } = "امور مالی و حسابداری";
    public string OrganizationAddress { get; set; } = "ملایر، بلوار انقلاب";
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationPostalCode { get; set; } = string.Empty;
    public string OrganizationPhoneFax { get; set; } = string.Empty;

    /// <summary>The public address of the site, used inside the QR code (for example https://example.ir).</summary>
    public string WebsiteBaseUrl { get; set; } = string.Empty;

    public string SignerName { get; set; } = string.Empty;
    public string SignerPosition { get; set; } = "معاون مدیر کل و مدیر اداره";

    /// <summary>The uploaded signature image as a data URI. Empty when none was uploaded.</summary>
    public string SignatureImageDataUri { get; set; } = string.Empty;

    // Editable titles of the three document kinds
    public string GuaranteeTitle { get; set; } = "ضمانت‌نامه";
    public string SalaryCertificateTitle { get; set; } = "گواهی کسر از حقوق";
    public string BankInquiryTitle { get; set; } = "گواهی استعلام بانک";

    // Credit rule
    /// <summary>The share of net pay an employee may request (70 means 70 percent).</summary>
    public decimal CreditPercent { get; set; } = 70m;

    /// <summary>Optional multiplier on top of the percent (1 means the ceiling is based on one month of net pay).</summary>
    public int CreditMonths { get; set; } = 1;

    /// <summary>The unit used to show and enter amounts. Amounts are always stored in Rials.</summary>
    public MoneyUnit MoneyUnit { get; set; } = MoneyUnit.Rial;

    /// <summary>Whether a request above the ceiling (surplus) may be registered at all.</summary>
    public bool AllowSurplus { get; set; } = true;

    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.SurplusOnly;

    /// <summary>The commitment text the applicant must accept for a surplus request.</summary>
    public string SurplusCommitmentText { get; set; } =
        "اینجانب متعهد می‌شوم که مبلغ مازاد بر سقف مجاز را به‌عنوان تعهد شخصی می‌پذیرم و مسئولیت آن بر عهده‌ی اینجانب است.";

    /// <summary>How many days a printed document stays valid (printed as the "valid until" date).</summary>
    public int ValidityDays { get; set; } = 30;

    // Document numbering: sequence.year.code
    public string DocumentNumberCode { get; set; } = "106";
    public string DocumentNumberSeparator { get; set; } = ".";

    // Passwords
    public bool RequireComplexPassword { get; set; }
    public bool ForcePasswordChangeOnFirstLogin { get; set; }

    // Lists
    public int DefaultPageSize { get; set; } = 50;
}
