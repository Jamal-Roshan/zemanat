using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Security;
using Zemanat.Core.Settings;
using Zemanat.Data.Entities;
using Zemanat.Data.Settings;

namespace Zemanat.Data.Seeding;

/// <summary>Creates the four roles, the default settings and the built-in admin account. Safe to run on every start.</summary>
public class DatabaseSeeder
{
    public const string DefaultAdminPassword = "admin";

    private readonly AppDbContext _db;
    private readonly RoleManager<AppRole> _roles;
    private readonly UserManager<AppUser> _users;
    private readonly SettingsStore _settings;

    public DatabaseSeeder(AppDbContext db, RoleManager<AppRole> roles, UserManager<AppUser> users, SettingsStore settings)
    {
        _db = db;
        _roles = roles;
        _users = users;
        _settings = settings;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync();
        await SeedSettingsAsync(ct);
        await SeedAdminAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var info in AppRoles.All)
        {
            var role = await _roles.FindByNameAsync(info.Name);
            if (role is null)
            {
                var created = await _roles.CreateAsync(new AppRole { Name = info.Name, Title = info.DefaultTitle, SortOrder = info.SortOrder });
                EnsureSucceeded(created, $"role {info.Name}");
            }
        }
    }

    private async Task SeedSettingsAsync(CancellationToken ct)
    {
        // Only fill the keys that are missing, so manager changes are never overwritten.
        var existing = await _db.Settings.Select(x => x.Key).ToListAsync(ct);
        var defaults = SystemSettingsSerializer.ToDictionary(new SystemSettings());
        foreach (var (key, value) in defaults.Where(d => !existing.Contains(d.Key)))
        {
            _db.Settings.Add(new SettingEntry { Key = key, Value = value });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedAdminAsync()
    {
        if (await _users.FindByNameAsync(AppRoles.AdminUserName) is not null)
        {
            return;
        }

        // The admin exists only while no real manager has been assigned.
        var managers = await _users.GetUsersInRoleAsync(AppRoles.Manager);
        if (managers.Count > 0)
        {
            return;
        }

        var admin = new AppUser
        {
            UserName = AppRoles.AdminUserName,
            FirstName = "مدیر",
            LastName = "سیستم",
            IsActive = true,
            LockoutEnabled = false
        };

        // The default password is set directly so password rules never block the built-in account.
        admin.PasswordHash = _users.PasswordHasher.HashPassword(admin, DefaultAdminPassword);
        EnsureSucceeded(await _users.CreateAsync(admin), "admin user");
        EnsureSucceeded(await _users.AddToRolesAsync(admin, AppRoles.All.Select(r => r.Name)), "admin roles");
    }

    private static void EnsureSucceeded(IdentityResult result, string what)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Seeding {what} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}
