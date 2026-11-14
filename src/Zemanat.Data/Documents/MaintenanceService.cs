using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zemanat.Data.Seeding;

namespace Zemanat.Data.Documents;

/// <summary>Dangerous operations that only a manager may start.</summary>
public class MaintenanceService
{
    private readonly AppDbContext _db;
    private readonly DatabaseSeeder _seeder;

    public MaintenanceService(AppDbContext db, DatabaseSeeder seeder)
    {
        _db = db;
        _seeder = seeder;
    }

    /// <summary>
    /// Empties every table (people, documents, inquiries, counters, templates, settings), then restores the default
    /// settings, the four roles and the admin account.
    /// </summary>
    public async Task WipeAllAsync(CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.BankInquiries.ExecuteDeleteAsync(ct);
        await _db.Documents.ExecuteDeleteAsync(ct);
        await _db.SecretariatCounters.ExecuteDeleteAsync(ct);
        await _db.PrintTemplates.ExecuteDeleteAsync(ct);
        await _db.Settings.ExecuteDeleteAsync(ct);
        await _db.Set<IdentityUserRole<int>>().ExecuteDeleteAsync(ct);
        await _db.Set<IdentityUserClaim<int>>().ExecuteDeleteAsync(ct);
        await _db.Set<IdentityUserLogin<int>>().ExecuteDeleteAsync(ct);
        await _db.Set<IdentityUserToken<int>>().ExecuteDeleteAsync(ct);
        await _db.Users.ExecuteDeleteAsync(ct);

        await tx.CommitAsync(ct);
        _db.ChangeTracker.Clear();

        await _seeder.SeedAsync(ct);
    }
}
