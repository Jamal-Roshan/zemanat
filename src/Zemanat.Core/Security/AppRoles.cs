namespace Zemanat.Core.Security;

/// <summary>A role with its default (editable) display title.</summary>
public sealed record RoleInfo(string Name, string DefaultTitle, int SortOrder);

/// <summary>The four roles. A person can hold several of them. Titles can be edited by the manager; the names never change.</summary>
public static class AppRoles
{
    public const string Manager = "Manager";
    public const string Operator = "Operator";
    public const string Approver = "Approver";
    public const string Employee = "Employee";

    /// <summary>The user name of the built-in account that exists until a real manager is assigned.</summary>
    public const string AdminUserName = "admin";

    public static readonly IReadOnlyList<RoleInfo> All =
    [
        new(Manager, "مدیر", 1),
        new(Operator, "اپراتور", 2),
        new(Approver, "تأییدکننده", 3),
        new(Employee, "کارمند", 4)
    ];
}
