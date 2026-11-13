using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Domain;
using Zemanat.Core.Personnel;
using Zemanat.Core.Security;
using Zemanat.Data;
using Zemanat.Data.Entities;
using Zemanat.Data.Personnel;
using Zemanat.Data.Seeding;
using Zemanat.Data.Users;

namespace Zemanat.Tests.Personnel;

public class PersonnelParserTests
{
    private static PersonnelRawRow Row(int n, string code = "1001", string first = "علی", string last = "رضایی",
        string national = "1234567890", string net = "100000000", string type = "رسمی", string unit = "واحد الف")
        => new(n, code, first, last, national, net, type, unit);

    [Fact]
    public void Valid_rows_are_cleaned()
    {
        var (rows, errors) = PersonnelRowParser.Parse([Row(2, code: "۱۰۰۱", first: "علي", type: " پيماني ", net: "۱۰۰٬۰۰۰")]);
        Assert.Empty(errors);
        var r = Assert.Single(rows);
        Assert.Equal("1001", r.Code);
        Assert.Equal("علی", r.FirstName);
        Assert.Equal(EmploymentType.Contractual, r.EmploymentType);
        Assert.Equal(100000, r.NetPayRial);
    }

    [Fact]
    public void Every_problem_is_reported_with_its_row()
    {
        var (rows, errors) = PersonnelRowParser.Parse([
            Row(2),
            Row(3, code: "1001"),            // duplicate
            Row(4, national: "123"),         // bad national code
            Row(5, net: "0"),                // bad pay
            Row(6, type: "قراردادی"),        // unknown type
            Row(7, first: "")]);             // missing name
        Assert.Single(rows);
        Assert.Equal([3, 4, 5, 6, 7], errors.Select(e => e.RowNumber).Distinct().ToArray());
    }
}

public sealed class ImportTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    public ImportTests()
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

    private static MemoryStream Workbook(params string?[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        string[] headers = ["کد پرسنلی", "نام", "نام خانوادگی", "کد ملی", "خالص پرداختی", "نوع استخدام", "واحد سازمانی"];
        for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
            {
                var v = rows[r][c];
                if (long.TryParse(v, out var n) && c is 0 or 4) ws.Cell(r + 2, c + 1).Value = n;
                else ws.Cell(r + 2, c + 1).Value = v;
            }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static string?[] P(string code, string first = "نام", string net = "50000000", string type = "رسمی")
        => [code, first, "آزمایشی‌زاده", "0000000000", net, type, "واحد"];

    [Fact]
    public async Task Import_adds_people_with_employee_role_and_code_as_password()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        var result = await import.ImportAsync(Workbook(P("1001"), P("1002", type: "پيماني ")));

        Assert.True(result.Success);
        Assert.Equal(2, result.Added);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByNameAsync("1002");
        Assert.True(await users.CheckPasswordAsync(user!, "1002"));
        Assert.Equal([AppRoles.Employee], await users.GetRolesAsync(user!));
        Assert.Equal(EmploymentType.Contractual, user!.EmploymentType);
    }

    [Fact]
    public async Task Errors_save_nothing()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        var result = await import.ImportAsync(Workbook(P("1001"), P("1002", net: "abc")));

        Assert.False(result.Success);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Users.CountAsync()); // only admin
    }

    [Fact]
    public async Task Second_import_updates_keeps_passwords_and_deactivates_missing()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await import.ImportAsync(Workbook(P("1001"), P("1002")));
        var first = await users.FindByNameAsync("1001");
        await users.RemovePasswordAsync(first!);
        await users.AddPasswordAsync(first!, "changed-pass");

        var second = await import.ImportAsync(Workbook(P("1001", first: "جدید")));
        Assert.Equal(0, second.Added);
        Assert.Equal(1, second.Deactivated);

        first = await users.FindByNameAsync("1001");
        Assert.Equal("جدید", first!.FirstName);
        Assert.True(await users.CheckPasswordAsync(first, "changed-pass"));
        var gone = await users.FindByNameAsync("1002");
        Assert.False(gone!.IsActive);
        Assert.Equal(DeactivationKind.System, gone.DeactivationKind);

        var third = await import.ImportAsync(Workbook(P("1001"), P("1002")));
        Assert.Equal(1, third.Reactivated);
        Assert.True((await users.FindByNameAsync("1002"))!.IsActive);
    }

    [Fact]
    public async Task Operator_deactivation_survives_import()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        var admin = scope.ServiceProvider.GetRequiredService<UserAdminService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await import.ImportAsync(Workbook(P("1001")));
        var user = await users.FindByNameAsync("1001");
        Assert.Null(await admin.SetActiveAsync(user!.Id, false, "مرخصی بدون حقوق"));
        await import.ImportAsync(Workbook(P("1001")));

        Assert.False((await users.FindByNameAsync("1001"))!.IsActive);
    }

    [Fact]
    public async Task Missing_header_is_reported()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        using var wb = new XLWorkbook();
        wb.AddWorksheet("S").Cell(1, 1).Value = "x";
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = await import.ImportAsync(ms);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("کد پرسنلی"));
    }

    [Fact]
    public async Task Real_manager_deactivates_admin_and_removing_role_brings_it_back()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        var admin = scope.ServiceProvider.GetRequiredService<UserAdminService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await import.ImportAsync(Workbook(P("1001")));
        var person = await users.FindByNameAsync("1001");

        Assert.Null(await admin.SetRolesAsync(person!.Id, [AppRoles.Manager, AppRoles.Employee]));
        Assert.False((await users.FindByNameAsync("admin"))!.IsActive);

        Assert.Null(await admin.SetRolesAsync(person.Id, [AppRoles.Employee]));
        Assert.True((await users.FindByNameAsync("admin"))!.IsActive);
    }

    [Fact]
    public async Task Search_filters_by_text_and_role()
    {
        using var scope = _provider.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<PersonnelImportService>();
        var admin = scope.ServiceProvider.GetRequiredService<UserAdminService>();
        await import.ImportAsync(Workbook(P("1001"), P("1002", first: "مریم")));

        var byText = await admin.SearchAsync("مریم", null, null, 1, 50);
        Assert.Equal(1, byText.Total);
        var byRole = await admin.SearchAsync(null, AppRoles.Employee, null, 1, 50);
        Assert.Equal(3, byRole.Total); // two people + admin
    }
}
