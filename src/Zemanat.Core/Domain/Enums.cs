namespace Zemanat.Core.Domain;

/// <summary>The two kinds of issued documents. Bank inquiries are a separate record attached to a document.</summary>
public enum DocumentType
{
    /// <summary>ضمانت‌نامه</summary>
    Guarantee = 1,

    /// <summary>گواهی کسر از حقوق</summary>
    SalaryCertificate = 2
}

/// <summary>The life cycle of a document. Settled and cancelled are final and cannot be undone.</summary>
public enum DocumentStatus
{
    /// <summary>Waiting for an approver. It already counts against the employee's ceiling.</summary>
    PendingApproval = 1,

    /// <summary>Approved (or issued directly) and numbered; it can be printed.</summary>
    Issued = 2,

    /// <summary>Rejected by an approver. It no longer counts against the ceiling.</summary>
    Rejected = 3,

    /// <summary>Settled (the loan ended normally).</summary>
    Settled = 4,

    /// <summary>Cancelled (for example because of a mistake).</summary>
    Cancelled = 5
}

/// <summary>Which documents need an approver before they get a number. A surplus request is only exempt when the mode is None.</summary>
public enum ApprovalMode
{
    /// <summary>No document needs approval, not even surplus ones.</summary>
    None = 0,

    /// <summary>Only surplus documents need approval (default).</summary>
    SurplusOnly = 1
}

/// <summary>The employment type read from the personnel Excel file.</summary>
public enum EmploymentType
{
    /// <summary>رسمی</summary>
    Official = 1,

    /// <summary>آزمایشی</summary>
    Probationary = 2,

    /// <summary>پیمانی</summary>
    Contractual = 3
}

/// <summary>Why a person is inactive.</summary>
public enum DeactivationKind
{
    None = 0,

    /// <summary>The person was missing from the latest personnel Excel upload (retired or left). Only the upload can undo it.</summary>
    System = 1,

    /// <summary>An operator deactivated the person by hand, with a reason.</summary>
    Operator = 2
}

/// <summary>The kinds of print templates. One shared letterhead is used by all documents.</summary>
public enum TemplateKind
{
    Letterhead = 1,
    Guarantee = 2,
    SalaryCertificate = 3,
    BankInquiry = 4,
    Authenticity = 5
}
