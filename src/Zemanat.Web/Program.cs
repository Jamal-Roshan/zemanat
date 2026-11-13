using Microsoft.EntityFrameworkCore;
using Zemanat.Data;
using Zemanat.Data.Seeding;
using Zemanat.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The connection string comes only from the ZEMANAT_CONNECTION_STRING environment variable.
builder.Services.AddZemanatData(o => o.UseSqlServer(ConnectionStringProvider.Get()));

var app = builder.Build();

// Migrations are applied on start when Database:AutoMigrate is true (default true), then the seeder runs.
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
