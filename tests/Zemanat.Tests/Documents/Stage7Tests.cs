using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Domain;
using Zemanat.Core.Security;
using Zemanat.Data;
using Zemanat.Data.Documents;
using Zemanat.Data.Entities;
using Zemanat.Data.Seeding;

namespace Zemanat.Tests.Documents;

public sealed class Stage7Tests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    public Stage7Tests()
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
    [InlineData(TemplateKind.Guarantee)]
    [InlineData(TemplateKind.SalaryCertificate)]
    [InlineData(TemplateKind.BankInquiry)]
    public void Default_templates_have_no_unknown_placeholders_and_render_a_sample(TemplateKind kind)
    {
        var html = TemplateService.DefaultHtml(kind)!;
        Assert.Empty(TemplateService.UnknownPlaceholders(kind, html));
        Assert.DoesNotContain("{{", TemplateService.RenderSample(kind, html));
    }

    [Fact]
    public void Unknown_placeholder_is_reported()
    {
        Assert.Equal(new[] { "Nope" }, TemplateService.UnknownPlaceholders(TemplateKind.Guarantee, "{{FullName}} {{Nope}}"));
    }

    [Fact]
    public async Task Wipe_empties_everything_and_reseeds_admin()
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new AppUser { UserName = "3001", FirstName = "a", LastName = "b", NationalCode = "0000000003", IsActive = true });
        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<TemplateService>().SaveAsync(TemplateKind.Guarantee, "x");

        await scope.ServiceProvider.GetRequiredService<MaintenanceService>().WipeAllAsync();

        var users = await db.Users.Select(u => u.UserName).ToListAsync();
        Assert.Equal(new[] { AppRoles.AdminUserName }, users);
        Assert.Empty(db.PrintTemplates);
        Assert.NotEmpty(db.Settings);
        Assert.NotEmpty(db.Roles);
    }
}
