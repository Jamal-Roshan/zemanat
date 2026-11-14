using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Domain;
using Zemanat.Core.Security;
using Zemanat.Data.Entities;

namespace Zemanat.Data.Documents;

/// <summary>Made-up people and documents so the system can be tried out. Every person here is fictional.</summary>
public class SampleDataService
{
    private static readonly string[] FirstNames = { "علی", "مریم", "حسین", "زهرا", "محمد", "فاطمه", "رضا", "سارا", "مهدی", "نرگس", "احمد", "لیلا" };
    private static readonly string[] LastNames = { "نمونه‌ای", "آزمایشی", "فرضی", "تستی", "مثالی", "دمویی", "الف", "ب", "ج", "د", "ه", "و" };
    private static readonly string[] Units = { "دبستان نمونه", "دبیرستان آزمایشی", "ستاد", "آموزشگاه فرضی" };

    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly DocumentService _documents;

    public SampleDataService(AppDbContext db, UserManager<AppUser> users, DocumentService documents)
    {
        _db = db;
        _users = users;
        _documents = documents;
    }

    /// <summary>Adds 12 sample employees (codes 9001-9012, password = code) and a few documents. Returns an error text or null.</summary>
    public async Task<string?> AddAsync(CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == "9001", ct)) return "داده‌ی نمونه قبلاً اضافه شده است.";

        var employeeRole = await _db.Roles.FirstAsync(r => r.Name == AppRoles.Employee, ct);
        var people = new List<AppUser>();
        for (var i = 0; i < FirstNames.Length; i++)
        {
            people.Add(new AppUser
            {
                UserName = (9001 + i).ToString(),
                NormalizedUserName = (9001 + i).ToString(),
                FirstName = FirstNames[i],
                LastName = LastNames[i],
                NationalCode = (9000000001L + i).ToString("D10"),
                OrgUnit = Units[i % Units.Length],
                EmploymentType = (EmploymentType)(i % 3 + 1),
                NetPayRial = 80_000_000 + i * 10_000_000L,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            });
        }

        Parallel.ForEach(people, u => u.PasswordHash = _users.PasswordHasher.HashPassword(u, u.UserName!));
        _db.Users.AddRange(people);
        await _db.SaveChangesAsync(ct);
        _db.UserRoles.AddRange(people.Select(u => new IdentityUserRole<int> { UserId = u.Id, RoleId = employeeRole.Id }));
        await _db.SaveChangesAsync(ct);

        // Two issued documents and one surplus request that waits for an approver.
        await _documents.CreateAsync(people[0].Id, false, people[0].Id, DocumentType.Guarantee, 20_000_000, "بانک نمونه", "نمونه", false);
        await _documents.CreateAsync(people[1].Id, false, people[1].Id, DocumentType.SalaryCertificate, 15_000_000, "بانک آزمایشی", "نمونه", false);
        await _documents.CreateAsync(people[2].Id, false, people[2].Id, DocumentType.Guarantee, 150_000_000, "بانک نمونه", "نمونه (مازاد)", true);
        return null;
    }
}
