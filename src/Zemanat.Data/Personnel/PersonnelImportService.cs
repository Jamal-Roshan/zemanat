using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Domain;
using Zemanat.Core.Personnel;
using Zemanat.Core.Security;
using Zemanat.Data.Entities;
using Zemanat.Data.Settings;
using Zemanat.Data.Users;

namespace Zemanat.Data.Personnel;

public sealed record PersonnelImportResult(
    IReadOnlyList<PersonnelError> Errors, int Added, int Updated, int Reactivated, int Deactivated)
{
    public bool Success => Errors.Count == 0;
}

/// <summary>
/// Imports the personnel Excel file. The whole file is checked first; if anything is wrong nothing is saved.
/// People missing from the file become inactive (reason: system); passwords of existing people are never touched.
/// </summary>
public class PersonnelImportService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly SettingsStore _settings;
    private readonly UserAdminService _admin;

    public PersonnelImportService(AppDbContext db, UserManager<AppUser> users, SettingsStore settings, UserAdminService admin)
    {
        _db = db;
        _users = users;
        _settings = settings;
        _admin = admin;
    }

    public async Task<PersonnelImportResult> ImportAsync(Stream excel, CancellationToken ct = default)
    {
        var (raw, readErrors) = PersonnelExcelReader.Read(excel);
        if (readErrors.Count > 0)
        {
            return new(readErrors, 0, 0, 0, 0);
        }

        var (rows, errors) = PersonnelRowParser.Parse(raw);
        if (errors.Count > 0)
        {
            return new(errors, 0, 0, 0, 0);
        }

        var settings = await _settings.LoadAsync(ct);
        var employeeRole = await _db.Roles.FirstAsync(r => r.Name == AppRoles.Employee, ct);
        var existing = await _db.Users.Where(u => u.UserName != AppRoles.AdminUserName)
            .ToDictionaryAsync(u => u.UserName!, ct);

        int added = 0, updated = 0, reactivated = 0, deactivated = 0;
        var fileCodes = rows.Select(r => r.Code).ToHashSet();
        var newUsers = new List<AppUser>();

        foreach (var row in rows)
        {
            if (existing.TryGetValue(row.Code, out var user))
            {
                user.FirstName = row.FirstName;
                user.LastName = row.LastName;
                user.NationalCode = row.NationalCode;
                user.NetPayRial = row.NetPayRial;
                user.EmploymentType = row.EmploymentType;
                user.OrgUnit = row.OrgUnit;
                if (!user.IsActive && user.DeactivationKind == DeactivationKind.System)
                {
                    user.IsActive = true;
                    user.DeactivationKind = DeactivationKind.None;
                    user.DeactivationReason = null;
                    reactivated++;
                }

                updated++;
            }
            else
            {
                newUsers.Add(new AppUser
                {
                    UserName = row.Code,
                    NormalizedUserName = _users.NormalizeName(row.Code),
                    FirstName = row.FirstName,
                    LastName = row.LastName,
                    NationalCode = row.NationalCode,
                    NetPayRial = row.NetPayRial,
                    EmploymentType = row.EmploymentType,
                    OrgUnit = row.OrgUnit,
                    IsActive = true,
                    LockoutEnabled = true,
                    MustChangePassword = settings.ForcePasswordChangeOnFirstLogin,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N")
                });
                added++;
            }
        }

        // The initial password is the personnel code. Hashing is slow on purpose, so spread it over all CPU cores.
        Parallel.ForEach(newUsers, u => u.PasswordHash = _users.PasswordHasher.HashPassword(u, u.UserName!));

        foreach (var user in existing.Values.Where(u => !fileCodes.Contains(u.UserName!) && u.IsActive))
        {
            user.IsActive = false;
            user.DeactivationKind = DeactivationKind.System;
            user.DeactivationReason = "نبودن در آخرین فایل اکسل پرسنل";
            deactivated++;
        }

        // One transaction: either the whole file is applied or nothing.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Users.AddRange(newUsers);
        await _db.SaveChangesAsync(ct);
        _db.UserRoles.AddRange(newUsers.Select(u => new IdentityUserRole<int> { UserId = u.Id, RoleId = employeeRole.Id }));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _admin.SyncAdminAsync(ct);
        return new(errors, added, updated, reactivated, deactivated);
    }
}
