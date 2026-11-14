using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Documents;
using Zemanat.Core.Domain;
using Zemanat.Core.Settings;
using Zemanat.Data;
using Zemanat.Data.Documents;
using Zemanat.Data.Entities;
using Zemanat.Data.Seeding;
using Zemanat.Data.Settings;

namespace Zemanat.Tests.Documents;

public class DocumentRulesTests
{
    [Theory]
    [InlineData(ApprovalMode.SurplusOnly, true, true)]
    [InlineData(ApprovalMode.SurplusOnly, false, false)]
    [InlineData(ApprovalMode.None, true, false)]
    [InlineData(ApprovalMode.None, false, false)]
    public void Approval_rule(ApprovalMode mode, bool surplus, bool expected)
        => Assert.Equal(expected, DocumentRules.NeedsApproval(mode, surplus));

    [Fact]
    public void Only_pending_and_issued_count_against_ceiling()
    {
        Assert.True(DocumentRules.CountsAgainstCeiling(DocumentStatus.PendingApproval));
        Assert.True(DocumentRules.CountsAgainstCeiling(DocumentStatus.Issued));
        Assert.False(DocumentRules.CountsAgainstCeiling(DocumentStatus.Rejected));
        Assert.False(DocumentRules.CountsAgainstCeiling(DocumentStatus.Settled));
        Assert.False(DocumentRules.CountsAgainstCeiling(DocumentStatus.Cancelled));
    }
}

public sealed class DocumentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly int _personId;

    public DocumentServiceTests()
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

        var person = new AppUser { UserName = "1001", FirstName = "نام", LastName = "آزمایشی", NationalCode = "0000000000", NetPayRial = 100_000_000, IsActive = true };
        db.Users.Add(person);
        db.SaveChanges();
        _personId = person.Id;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private (DocumentService Service, SettingsStore Settings, IServiceScope Scope) Create()
    {
        var scope = _provider.CreateScope();
        return (scope.ServiceProvider.GetRequiredService<DocumentService>(), scope.ServiceProvider.GetRequiredService<SettingsStore>(), scope);
    }

    [Fact]
    public async Task Normal_request_is_issued_with_number_and_tracking_code()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        var result = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 50_000_000, "بانک نمونه", null, false);

        Assert.True(result.Success, result.Error);
        var doc = result.Document!;
        Assert.Equal(DocumentStatus.Issued, doc.Status);
        Assert.Equal(1, doc.Sequence);
        Assert.EndsWith(".106", doc.NumberText);
        Assert.StartsWith("1.", doc.NumberText);
        Assert.True(TrackingCode.IsWellFormed(doc.TrackingCode));
        Assert.Equal(70_000_000, doc.CapRial);
    }

    [Fact]
    public async Task Numbers_increase_for_each_issued_document()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        var a = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 10_000_000, "بانک", null, false);
        var b = await service.CreateAsync(1, false, _personId, DocumentType.SalaryCertificate, 10_000_000, "بانک", null, false);
        Assert.Equal(1, a.Document!.Sequence);
        Assert.Equal(2, b.Document!.Sequence);
        Assert.NotEqual(a.Document.TrackingCode, b.Document.TrackingCode);
    }

    [Fact]
    public async Task Surplus_needs_commitment_and_waits_for_approval()
    {
        var (service, _, scope) = Create();
        using var _ = scope;

        var noCommit = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 80_000_000, "بانک", null, false);
        Assert.False(noCommit.Success);

        var ok = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 80_000_000, "بانک", null, true);
        Assert.True(ok.Success, ok.Error);
        Assert.Equal(DocumentStatus.PendingApproval, ok.Document!.Status);
        Assert.True(ok.Document.IsSurplus);
        Assert.Equal(10_000_000, ok.Document.ExcessAmountRial);
        Assert.Null(ok.Document.NumberText);
    }

    [Fact]
    public async Task Manager_surplus_is_issued_directly()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        var result = await service.CreateAsync(1, true, _personId, DocumentType.Guarantee, 80_000_000, "بانک", null, true);
        Assert.Equal(DocumentStatus.Issued, result.Document!.Status);
        Assert.Equal(1, result.Document.DecidedByUserId);
    }

    [Fact]
    public async Task Mode_none_issues_surplus_without_approval()
    {
        var (service, settingsStore, scope) = Create();
        using var _ = scope;
        var s = await settingsStore.LoadAsync();
        s.ApprovalMode = ApprovalMode.None;
        await settingsStore.SaveAsync(s);

        var result = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 80_000_000, "بانک", null, true);
        Assert.Equal(DocumentStatus.Issued, result.Document!.Status);
    }

    [Fact]
    public async Task Surplus_can_be_switched_off()
    {
        var (service, settingsStore, scope) = Create();
        using var _ = scope;
        var s = await settingsStore.LoadAsync();
        s.AllowSurplus = false;
        await settingsStore.SaveAsync(s);

        var result = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 80_000_000, "بانک", null, true);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Pending_and_issued_use_the_ceiling_but_rejected_frees_it()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        var first = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 60_000_000, "بانک", null, false);
        var preview = await service.PreviewAsync(_personId, 0);
        Assert.Equal(10_000_000, preview!.Remaining);

        var pending = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 20_000_000, "بانک", null, true);
        Assert.Equal(DocumentStatus.PendingApproval, pending.Document!.Status);
        Assert.Equal(0, (await service.PreviewAsync(_personId, 0))!.Remaining);

        Assert.False((await service.RejectAsync(pending.Document.Id, 2, "")).Success);
        Assert.True((await service.RejectAsync(pending.Document.Id, 2, "نامعتبر")).Success);
        Assert.Equal(10_000_000, (await service.PreviewAsync(_personId, 0))!.Remaining);
        Assert.NotNull(first.Document);
    }

    [Fact]
    public async Task Approve_issues_once_and_gives_the_next_number()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        var pending = await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 80_000_000, "بانک", null, true);

        var approved = await service.ApproveAsync(pending.Document!.Id, 2, "تأیید");
        Assert.True(approved.Success, approved.Error);
        Assert.Equal(DocumentStatus.Issued, approved.Document!.Status);
        Assert.Equal(1, approved.Document.Sequence);

        Assert.False((await service.ApproveAsync(pending.Document.Id, 2, null)).Success);
    }

    [Fact]
    public async Task Inactive_person_and_bad_input_are_refused()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        Assert.False((await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 0, "بانک", null, false)).Success);
        Assert.False((await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 1000, "  ", null, false)).Success);
        Assert.False((await service.CreateAsync(1, false, 99999, DocumentType.Guarantee, 1000, "بانک", null, false)).Success);
    }

    [Fact]
    public async Task Search_filters_by_person_and_status()
    {
        var (service, _, scope) = Create();
        using var _ = scope;
        await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 10_000_000, "بانک ملی", null, false);
        await service.CreateAsync(1, false, _personId, DocumentType.Guarantee, 90_000_000, "بانک ملت", null, true);

        Assert.Equal(2, (await service.SearchAsync(new DocumentFilter(PersonId: _personId), 1, 50)).Total);
        Assert.Equal(1, (await service.SearchAsync(new DocumentFilter(Status: DocumentStatus.PendingApproval), 1, 50)).Total);
        Assert.Equal(1, (await service.SearchAsync(new DocumentFilter(Text: "ملت"), 1, 50)).Total);
        Assert.Equal(0, (await service.SearchAsync(new DocumentFilter(PersonId: 12345), 1, 50)).Total);
    }
}
