namespace Zemanat.Data.Documents;

/// <summary>One line of the audit trail: who did what and when.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public DateTime TimeUtc { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public static class Audit
{
    /// <summary>Adds an entry to the context without saving, so it commits together with the change it describes.</summary>
    public static void Add(AppDbContext db, int? userId, string action, string details)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TimeUtc = DateTime.UtcNow,
            UserId = userId,
            Action = action,
            Details = details.Length > 500 ? details[..500] : details
        });
    }
}
