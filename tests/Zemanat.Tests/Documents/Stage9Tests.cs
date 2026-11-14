using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Domain;
using Zemanat.Data;
using Zemanat.Data.Documents;
using Zemanat.Data.Entities;
using Zemanat.Data.Seeding;

namespace Zemanat.Tests.Documents;

public sealed class Stage9Tests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly int _personId;

    public Stage9Tests()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZemanatData(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync().GetAwaiter().GetResult();
        var p = new AppUser { UserName = "3001", FirstName = "نام", LastName = "ج", NationalCode = "0000000003", NetPayRial = 100_000_000, IsActive = true };
        db.Users.Add(p);
        db.SaveChanges();
        _personId = p.Id;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Submit_approve_and_reject_publish_events_to_subscribers()
    {
        var hub = _provider.GetRequiredService<DocumentEventHub>();
        var seen = new List<DocumentEvent>();
        hub.Published += seen.Add;
        hub.Published += _ => throw new InvalidOperationException("a closed tab must not break the others");

        using var scope = _provider.CreateScope();
        var docs = scope.ServiceProvider.GetRequiredService<DocumentService>();

        var issued = await docs.CreateAsync(7, false, _personId, DocumentType.Guarantee, 1_000_000, "بانک", null, false);
        Assert.Equal(DocumentEventKind.Issued, seen[^1].Kind);
        await docs.SettleAsync(issued.Document!.Id, 7);
        Assert.Equal(DocumentEventKind.Settled, seen[^1].Kind);

        var pending = await docs.CreateAsync(7, false, _personId, DocumentType.Guarantee, 90_000_000, "بانک", null, true);
        Assert.Equal(DocumentEventKind.Submitted, seen[^1].Kind);
        Assert.Equal(7, seen[^1].CreatedByUserId);
        Assert.Equal(_personId, seen[^1].PersonId);

        await docs.ApproveAsync(pending.Document!.Id, 2, null);
        Assert.Equal(DocumentEventKind.Approved, seen[^1].Kind);
        Assert.Equal(2, seen[^1].ActorUserId);
        Assert.NotNull(seen[^1].NumberText);

        var second = await docs.CreateAsync(7, false, _personId, DocumentType.Guarantee, 90_000_000, "بانک", null, true);
        await docs.RejectAsync(second.Document!.Id, 2, "دلیل");
        Assert.Equal(DocumentEventKind.Rejected, seen[^1].Kind);

    }

    [Fact]
    public async Task Sample_data_is_added_once_with_a_pending_request()
    {
        using var scope = _provider.CreateScope();
        var sample = scope.ServiceProvider.GetRequiredService<SampleDataService>();
        Assert.Null(await sample.AddAsync());
        Assert.NotNull(await sample.AddAsync());

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(12, await db.Users.CountAsync(u => u.UserName!.StartsWith("90")));
        Assert.Equal(2, await db.Documents.CountAsync(d => d.Status == DocumentStatus.Issued));
        Assert.Equal(1, await db.Documents.CountAsync(d => d.Status == DocumentStatus.PendingApproval));
    }
}

public sealed class SearchDigitTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    public SearchDigitTests()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZemanatData(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    [Theory]
    [InlineData("3001")]
    [InlineData("۳۰۰۱")]
    [InlineData("شعبه 12")]
    [InlineData("شعبه ۱۲")]
    [InlineData("0000000003")]
    [InlineData("۰۰۰۰۰۰۰۰۰۳")]
    public async Task Search_finds_records_with_latin_or_persian_digits(string query)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var person = new AppUser { UserName = "3001", FirstName = "نام", LastName = "ج", NationalCode = "0000000003", OrgUnit = "دبستان ۱۲", NetPayRial = 100_000_000, IsActive = true };
        db.Users.Add(person);
        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<DocumentService>()
            .CreateAsync(1, false, person.Id, DocumentType.Guarantee, 1_000_000, "بانک شعبه ۱۲", null, false);

        var docs = await scope.ServiceProvider.GetRequiredService<DocumentService>().SearchAsync(new DocumentFilter(query), 1, 10);
        Assert.Single(docs.Items);

        if (!query.Contains("شعبه"))
        {
            var users = await scope.ServiceProvider.GetRequiredService<Zemanat.Data.Users.UserAdminService>().SearchAsync(query, null, null, 1, 10);
            Assert.Single(users.Items);
        }
    }
}
