using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zemanat.Data.Documents;
using Zemanat.Data.Entities;

namespace Zemanat.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SettingEntry> Settings => Set<SettingEntry>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<BankInquiry> BankInquiries => Set<BankInquiry>();
    public DbSet<PrintTemplate> PrintTemplates => Set<PrintTemplate>();
    public DbSet<SecretariatCounter> SecretariatCounters => Set<SecretariatCounter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.NationalCode).HasMaxLength(10);
            e.Property(x => x.OrgUnit).HasMaxLength(200);
            e.Property(x => x.DeactivationReason).HasMaxLength(500);
            e.HasIndex(x => x.NationalCode);
        });

        builder.Entity<AppRole>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(100);
        });

        builder.Entity<SecretariatCounter>(e => e.HasKey(x => x.Year));
        builder.Entity<BankInquiry>(e =>
        {
            e.Property(x => x.BankLetterNumber).HasMaxLength(50);
            e.Property(x => x.BorrowerName).HasMaxLength(200);
            e.Property(x => x.BorrowerFatherName).HasMaxLength(100);
            e.Property(x => x.NumberText).HasMaxLength(50);
            e.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DocumentId).IsUnique();
            e.HasIndex(x => new { x.SequenceYear, x.Sequence }).IsUnique();
        });
        builder.Entity<PrintTemplate>(e =>
        {
            e.HasIndex(x => x.Kind).IsUnique();
        });

        builder.Entity<Document>(e =>
        {
            e.Property(x => x.Beneficiary).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.PersonnelCode).HasMaxLength(20);
            e.Property(x => x.PersonName).HasMaxLength(200);
            e.Property(x => x.NationalCode).HasMaxLength(10);
            e.Property(x => x.OrgUnit).HasMaxLength(200);
            e.Property(x => x.NumberText).HasMaxLength(50);
            e.Property(x => x.TrackingCode).HasMaxLength(12);
            e.Property(x => x.DecisionNote).HasMaxLength(500);
            e.Property(x => x.ClosedNote).HasMaxLength(500);
            e.Property(x => x.FatherName).HasMaxLength(100);
            e.Property(x => x.GenderPrefix).HasMaxLength(10);
            e.Property(x => x.LoanOwnerName).HasMaxLength(200);
            e.Property(x => x.LoanOwnerFatherName).HasMaxLength(100);
            e.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.TrackingCode).IsUnique();
            e.HasIndex(x => new { x.SequenceYear, x.Sequence }).IsUnique();
            e.HasIndex(x => new { x.PersonId, x.Status });
            e.HasIndex(x => x.Status);
        });

        builder.Entity<SettingEntry>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
        });
    }
}
