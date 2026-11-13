using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Domain;
using Zemanat.Core.Security;
using Zemanat.Data.Entities;
using Zemanat.Data.Settings;

namespace Zemanat.Data.Users;

public sealed record UserListItem(int Id, string Code, string FullName, string OrgUnit, EmploymentType EmploymentType,
    bool IsActive, DeactivationKind DeactivationKind, string? DeactivationReason, IReadOnlyList<string> Roles);

public sealed record UserPage(IReadOnlyList<UserListItem> Items, int Total);

/// <summary>User and role management: search, roles, activate/deactivate, password reset and change.</summary>
public class UserAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly SettingsStore _settings;

    public UserAdminService(AppDbContext db, UserManager<AppUser> users, SettingsStore settings)
    {
        _db = db;
        _users = users;
        _settings = settings;
    }

    public async Task<UserPage> SearchAsync(string? query, string? role, bool? active, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Users.AsNoTracking().AsQueryable();
        var text = Core.Text.PersianText.ToLatinDigits(Core.Text.PersianText.CleanText(query));
        if (text.Length > 0)
        {
            q = q.Where(u => u.UserName!.Contains(text) || u.FirstName.Contains(text) || u.LastName.Contains(text)
                             || u.NationalCode.Contains(text) || u.OrgUnit.Contains(text));
        }

        if (active is not null)
        {
            q = q.Where(u => u.IsActive == active);
        }

        if (!string.IsNullOrEmpty(role))
        {
            q = q.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }

        var total = await q.CountAsync(ct);
        var users = await q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.UserName)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var ids = users.Select(u => u.Id).ToList();
        var roleRows = await (from ur in _db.UserRoles
                              join r in _db.Roles on ur.RoleId equals r.Id
                              where ids.Contains(ur.UserId)
                              select new { ur.UserId, r.Name }).ToListAsync(ct);
        var roleMap = roleRows.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name!).ToList());

        var items = users.Select(u => new UserListItem(u.Id, u.UserName!, u.FullName, u.OrgUnit, u.EmploymentType, u.IsActive,
            u.DeactivationKind, u.DeactivationReason, roleMap.GetValueOrDefault(u.Id, Array.Empty<string>()))).ToList();
        return new UserPage(items, total);
    }

    /// <summary>Replaces the person's roles. The built-in admin keeps all roles; at most the four known roles are allowed.</summary>
    public async Task<string?> SetRolesAsync(int userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return "کاربر پیدا نشد.";
        if (user.UserName == AppRoles.AdminUserName) return "نقش‌های کاربر admin قابل تغییر نیست.";

        var wanted = roles.Where(r => AppRoles.All.Any(a => a.Name == r)).Distinct().ToList();
        var current = await _users.GetRolesAsync(user);
        var remove = await _users.RemoveFromRolesAsync(user, current.Except(wanted));
        if (!remove.Succeeded) return string.Join("؛ ", remove.Errors.Select(e => e.Description));
        var add = await _users.AddToRolesAsync(user, wanted.Except(current));
        if (!add.Succeeded) return string.Join("؛ ", add.Errors.Select(e => e.Description));

        await SyncAdminAsync(ct);
        return null;
    }

    /// <summary>Deactivation by an operator needs a reason. Reactivating clears the reason. The admin account cannot be deactivated by hand.</summary>
    public async Task<string?> SetActiveAsync(int userId, bool active, string? reason, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return "کاربر پیدا نشد.";
        if (user.UserName == AppRoles.AdminUserName) return "کاربر admin را نمی‌توان دستی غیرفعال کرد.";

        if (active)
        {
            user.IsActive = true;
            user.DeactivationKind = DeactivationKind.None;
            user.DeactivationReason = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(reason)) return "دلیل غیرفعال‌سازی را بنویسید.";
            user.IsActive = false;
            user.DeactivationKind = DeactivationKind.Operator;
            user.DeactivationReason = Core.Text.PersianText.CleanText(reason);
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _users.UpdateAsync(user);
        await SyncAdminAsync(ct);
        return null;
    }

    /// <summary>Sets the password back to the personnel code and clears any lockout.</summary>
    public async Task<string?> ResetPasswordAsync(int userId)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return "کاربر پیدا نشد.";

        user.PasswordHash = _users.PasswordHasher.HashPassword(user, user.UserName!);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.MustChangePassword = (await _settings.LoadAsync()).ForcePasswordChangeOnFirstLogin;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        var result = await _users.UpdateAsync(user);
        return result.Succeeded ? null : string.Join("؛ ", result.Errors.Select(e => e.Description));
    }

    public async Task<string?> ChangePasswordAsync(int userId, string current, string next)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return "کاربر پیدا نشد.";

        var result = await _users.ChangePasswordAsync(user, current, next);
        if (!result.Succeeded) return string.Join("؛ ", result.Errors.Select(e => e.Description));

        user.MustChangePassword = false;
        await _users.UpdateAsync(user);
        return null;
    }

    /// <summary>
    /// The built-in admin is active only while no real, active manager exists.
    /// </summary>
    public async Task SyncAdminAsync(CancellationToken ct = default)
    {
        var admin = await _db.Users.FirstOrDefaultAsync(u => u.UserName == AppRoles.AdminUserName, ct);
        if (admin is null) return;

        var managerRoleId = await _db.Roles.Where(r => r.Name == AppRoles.Manager).Select(r => r.Id).FirstAsync(ct);
        var realManager = await (from ur in _db.UserRoles
                                 join u in _db.Users on ur.UserId equals u.Id
                                 where ur.RoleId == managerRoleId && u.IsActive && u.UserName != AppRoles.AdminUserName
                                 select u.Id).AnyAsync(ct);

        if (admin.IsActive == realManager)
        {
            admin.IsActive = !realManager;
            admin.DeactivationKind = realManager ? DeactivationKind.System : DeactivationKind.None;
            admin.DeactivationReason = realManager ? "مدیر واقعی تعیین شده است" : null;
            await _db.SaveChangesAsync(ct);
        }
    }
}
