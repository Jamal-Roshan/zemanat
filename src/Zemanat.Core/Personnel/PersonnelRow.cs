using Zemanat.Core.Domain;

namespace Zemanat.Core.Personnel;

/// <summary>A row exactly as read from the Excel file (text only).</summary>
public sealed record PersonnelRawRow(int RowNumber, string? Code, string? FirstName, string? LastName,
    string? NationalCode, string? NetPay, string? EmploymentType, string? OrgUnit);

/// <summary>A checked row ready to be saved. NetPay is in Rials.</summary>
public sealed record PersonnelRow(int RowNumber, string Code, string FirstName, string LastName,
    string NationalCode, long NetPayRial, EmploymentType EmploymentType, string OrgUnit);

/// <summary>One problem found in the file, with the Persian message shown to the operator.</summary>
public sealed record PersonnelError(int RowNumber, string Message);
