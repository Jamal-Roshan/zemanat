using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Domain;
using Zemanat.Core.Security;
using Zemanat.Core.Settings;
using Zemanat.Data;
using Zemanat.Data.Entities;
using Zemanat.Data.Seeding;
using Zemanat.Data.Settings;

namespace Zemanat.Tests.Data;

public sealed class DataTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    public DataTests()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZemanatData(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Seeder_creates_roles_settings_and_admin_with_all_roles()
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var admin = await users.FindByNameAsync("admin");
        Assert.NotNull(admin);
        Assert.True(await users.CheckPasswordAsync(admin!, "admin"));
        var roles = await users.GetRolesAsync(admin!);
        Assert.Equal(4, roles.Count);

        var settings = await scope.ServiceProvider.GetRequiredService<SettingsStore>().LoadAsync();
        Assert.Equal("106", settings.DocumentNumberCode);
        Assert.Equal(ApprovalMode.SurplusOnly, settings.ApprovalMode);
    }

    [Fact]
    public async Task Seeder_is_idempotent_and_keeps_changed_settings()
    {
        using var scope = _provider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        var store = scope.ServiceProvider.GetRequiredService<SettingsStore>();

        await seeder.SeedAsync();
        var settings = await store.LoadAsync();
        settings.CreditPercent = 60m;
        await store.SaveAsync(settings);
        await seeder.SeedAsync();

        Assert.Equal(60m, (await store.LoadAsync()).CreditPercent);
        Assert.Equal(4, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Roles.CountAsync());
    }

    [Fact]
    public async Task Complex_password_rule_is_off_by_default_and_works_when_on()
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var store = scope.ServiceProvider.GetRequiredService<SettingsStore>();

        var first = await users.CreateAsync(new AppUser { UserName = "1001" }, "1001");
        Assert.True(first.Succeeded);

        var settings = await store.LoadAsync();
        settings.RequireComplexPassword = true;
        await store.SaveAsync(settings);

        var second = await users.CreateAsync(new AppUser { UserName = "1002" }, "1002");
        Assert.False(second.Succeeded);
        var third = await users.CreateAsync(new AppUser { UserName = "1003" }, "abc12345");
        Assert.True(third.Succeeded);
    }

    [Fact]
    public async Task Lockout_follows_the_policy()
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = "2001" };
        await users.CreateAsync(user, "2001");

        for (var i = 0; i < PasswordPolicy.MaxFailedAttempts; i++)
        {
            await users.AccessFailedAsync(user);
        }

        Assert.True(await users.IsLockedOutAsync(user));
    }
}

public class SettingsTests
{
    [Fact]
    public void Serializer_round_trips_all_values()
    {
        var original = new SystemSettings { CreditPercent = 65.5m, CreditMonths = 2, AllowSurplus = false, ApprovalMode = ApprovalMode.None, DocumentNumberSeparator = "-" };
        var copy = SystemSettingsSerializer.FromDictionary(SystemSettingsSerializer.ToDictionary(original));
        Assert.Equal(65.5m, copy.CreditPercent);
        Assert.Equal(2, copy.CreditMonths);
        Assert.False(copy.AllowSurplus);
        Assert.Equal(ApprovalMode.None, copy.ApprovalMode);
        Assert.Equal("-", copy.DocumentNumberSeparator);
    }

    [Fact]
    public void Bad_values_keep_defaults()
    {
        var copy = SystemSettingsSerializer.FromDictionary(new Dictionary<string, string> { ["CreditPercent"] = "abc", ["ApprovalMode"] = "99" });
        Assert.Equal(70m, copy.CreditPercent);
        Assert.Equal(ApprovalMode.SurplusOnly, copy.ApprovalMode);
    }

    [Fact]
    public void Validator_accepts_defaults_and_rejects_bad_input()
    {
        Assert.Empty(SystemSettingsValidator.Validate(new SystemSettings()));
        Assert.NotEmpty(SystemSettingsValidator.Validate(new SystemSettings { CreditPercent = 0 }));
        Assert.NotEmpty(SystemSettingsValidator.Validate(new SystemSettings { DocumentNumberCode = "1.6", DocumentNumberSeparator = "." }));
        Assert.NotEmpty(SystemSettingsValidator.Validate(new SystemSettings { WebsiteBaseUrl = "abc" }));
    }

    [Theory]
    [InlineData("رسمی", true, EmploymentType.Official)]
    [InlineData(" پیمانی ", true, EmploymentType.Contractual)]
    [InlineData("آزمایشی", true, EmploymentType.Probationary)]
    [InlineData("قراردادی", false, default(EmploymentType))]
    public void EmploymentType_parses(string text, bool ok, EmploymentType expected)
    {
        Assert.Equal(ok, EmploymentTypes.TryParse(text, out var type));
        Assert.Equal(expected, type);
    }

    [Theory]
    [InlineData("abc12345", 0)]
    [InlineData("12345678", 1)]
    [InlineData("abc", 2)]
    public void PasswordPolicy_counts_errors(string password, int errors)
        => Assert.Equal(errors, PasswordPolicy.Validate(password).Count);
}
