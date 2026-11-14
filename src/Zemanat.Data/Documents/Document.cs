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
}

/// <summary>The last used secretariat sequence of one Jalali year. Shared by documents and bank inquiries.</summary>
public class SecretariatCounter
{
    public int Year { get; set; }
    public int LastSequence { get; set; }
}
