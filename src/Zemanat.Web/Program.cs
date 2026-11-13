using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Zemanat.Core.Security;
using Zemanat.Data;
using Zemanat.Data.Entities;
using Zemanat.Web.Auth;
using Zemanat.Data.Seeding;
using Zemanat.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The connection string comes only from the ZEMANAT_CONNECTION_STRING environment variable.
builder.Services.AddZemanatData(o => o.UseSqlServer(ConnectionStringProvider.Get()));

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, o =>
    {
        o.LoginPath = "/login";
        o.Cookie.Name = "Zemanat.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(PasswordPolicy.SessionMinutes);
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = async ctx =>
        {
            // A person who was deactivated is signed out on the next request.
            var users = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var user = await users.GetUserAsync(ctx.Principal!);
            if (user is null || !user.IsActive)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAccountEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
