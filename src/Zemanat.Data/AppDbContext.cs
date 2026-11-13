using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zemanat.Data.Entities;

namespace Zemanat.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SettingEntry> Settings => Set<SettingEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.NationalCode).HasMaxLength(10);
            e.Property(x => x.DeactivationReason).HasMaxLength(500);
            e.HasIndex(x => x.NationalCode);
        });

        builder.Entity<AppRole>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(100);
        });

        builder.Entity<SettingEntry>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
        });
    }
}
