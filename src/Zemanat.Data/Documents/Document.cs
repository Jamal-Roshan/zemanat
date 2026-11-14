using Zemanat.Core.Domain;
using Zemanat.Data.Entities;

namespace Zemanat.Data.Documents;

/// <summary>A guarantee or salary certificate. Person details are copied at request time so later Excel uploads never change old documents.</summary>
public class Document
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public AppUser? Person { get; set; }

    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }

    /// <summary>The requested amount in Rials.</summary>
    public long AmountRial { get; set; }

    /// <summary>The bank or organization the document is issued for.</summary>
    public string Beneficiary { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Printed on the forms
    public string? FatherName { get; set; }
    public string GenderPrefix { get; set; } = "آقای";
    public string? LoanOwnerName { get; set; }
    public string? LoanOwnerFatherName { get; set; }

    public bool IsSurplus { get; set; }
    public long ExcessAmountRial { get; set; }
    public bool SurplusCommitmentAccepted { get; set; }

    // Snapshot of the person at request time
    public string PersonnelCode { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public string NationalCode { get; set; } = string.Empty;
    public string OrgUnit { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public long NetPayRial { get; set; }
    public long CapRial { get; set; }

    // Number, given when the document is issued
    public int? Sequence { get; set; }
    public int? SequenceYear { get; set; }
    public string? NumberText { get; set; }
    public string TrackingCode { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public DateTime? IssuedUtc { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedUtc { get; set; }
    public string? DecisionNote { get; set; }

    /// <summary>Set when the document is settled or cancelled. Both are final.</summary>
    public DateTime? ClosedUtc { get; set; }
    public int? ClosedByUserId { get; set; }
    public string? ClosedNote { get; set; }
}

/// <summary>A bank inquiry answer (one per document). It takes its own secretariat number.</summary>
public class BankInquiry
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document? Document { get; set; }

    public string BankLetterNumber { get; set; } = string.Empty;
    public DateTime BankLetterDate { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string? BorrowerFatherName { get; set; }

    public int Sequence { get; set; }
    public int SequenceYear { get; set; }
    public string NumberText { get; set; } = string.Empty;

    public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
}

/// <summary>The last used secretariat sequence of one Jalali year. Shared by documents and bank inquiries.</summary>
public class SecretariatCounter
{
    public int Year { get; set; }
    public int LastSequence { get; set; }
}

/// <summary>An edited copy of a print template. When no row exists for a kind, the built-in default is used.</summary>
public class PrintTemplate
{
    public int Id { get; set; }
    public Zemanat.Core.Domain.TemplateKind Kind { get; set; }
    public string Html { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
