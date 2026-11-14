using Zemanat.Core.Domain;

namespace Zemanat.Core.Documents;

/// <summary>Titles and the approval rule for documents.</summary>
public static class DocumentRules
{
    /// <summary>
    /// Whether a new document must wait for an approver before it gets a number.
    /// Only the "surplus only" mode asks for approval, and only for surplus documents.
    /// </summary>
    public static bool NeedsApproval(ApprovalMode mode, bool isSurplus) => mode == ApprovalMode.SurplusOnly && isSurplus;

    /// <summary>Documents in these statuses count against the employee's ceiling.</summary>
    public static bool CountsAgainstCeiling(DocumentStatus status) =>
        status is DocumentStatus.PendingApproval or DocumentStatus.Issued;

    public static string Title(this DocumentStatus status) => status switch
    {
        DocumentStatus.PendingApproval => "در انتظار تأیید",
        DocumentStatus.Issued => "صادرشده",
        DocumentStatus.Rejected => "ردشده",
        DocumentStatus.Settled => "تسویه‌شده",
        DocumentStatus.Cancelled => "ابطال‌شده",
        _ => string.Empty
    };

    public static string Title(this DocumentType type) => type switch
    {
        DocumentType.Guarantee => "ضمانت‌نامه",
        DocumentType.SalaryCertificate => "گواهی کسر از حقوق",
        _ => string.Empty
    };
}
