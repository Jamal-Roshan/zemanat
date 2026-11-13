using Microsoft.AspNetCore.Identity;
using Zemanat.Core.Domain;

namespace Zemanat.Data.Entities;

/// <summary>A person who can sign in. The user name is the personnel code.</summary>
public class AppUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>The national code, used only for matching rows in the personnel Excel file.</summary>
    public string NationalCode { get; set; } = string.Empty;

    public EmploymentType EmploymentType { get; set; } = EmploymentType.Official;

    /// <summary>The monthly net pay in Rials.</summary>
    public long NetPayRial { get; set; }

    public bool IsActive { get; set; } = true;
    public DeactivationKind DeactivationKind { get; set; }
    public string? DeactivationReason { get; set; }

    public bool MustChangePassword { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
