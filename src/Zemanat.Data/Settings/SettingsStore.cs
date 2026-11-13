using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Settings;
using Zemanat.Data.Entities;
using Zemanat.Data.Identity;

namespace Zemanat.Data.Settings;

/// <summary>Loads and saves <see cref="SystemSettings"/> from the Settings table.</summary>
public class SettingsStore : IPasswordRuleSwitch
{
    private readonly AppDbContext _db;

    public SettingsStore(AppDbContext db) => _db = db;

    public async Task<SystemSettings> LoadAsync(CancellationToken ct = default)
    {
        var rows = await _db.Settings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        return SystemSettingsSerializer.FromDictionary(rows);
    }

    public async Task SaveAsync(SystemSettings settings, CancellationToken ct = default)
    {
        var existing = await _db.Settings.ToDictionaryAsync(x => x.Key, ct);
        foreach (var (key, value) in SystemSettingsSerializer.ToDictionary(settings))
        {
            if (existing.TryGetValue(key, out var row))
            {
                row.Value = value;
            }
            else
            {
                _db.Settings.Add(new Entities.SettingEntry { Key = key, Value = value });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsComplexRequiredAsync() => (await LoadAsync()).RequireComplexPassword;
}
