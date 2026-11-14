using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zemanat.Core.Security;
using Zemanat.Data.Entities;
using Zemanat.Data.Identity;
using Zemanat.Data.Seeding;
using Zemanat.Data.Settings;

namespace Zemanat.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Registers the database, Identity (password and lockout rules) and the seeder.</summary>
    public static IServiceCollection AddZemanatData(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<AppDbContext>(configureDb);
        services.AddScoped<SettingsStore>();
        services.AddScoped<IPasswordRuleSwitch>(sp => sp.GetRequiredService<SettingsStore>());
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<Users.UserAdminService>();
        services.AddScoped<Documents.DocumentService>();
        services.AddScoped<Documents.TemplateService>();
        services.AddScoped<Documents.InquiryService>();
        services.AddScoped<Documents.MaintenanceService>();
        services.AddScoped<Documents.DocumentPrintService>();
        services.AddScoped<Personnel.PersonnelImportService>();

        services.AddIdentityCore<AppUser>(o =>
            {
                // Real rules come from ConfigurablePasswordValidator; the built-in ones are off.
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 1;

                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = PasswordPolicy.MaxFailedAttempts;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(PasswordPolicy.LockoutMinutes);

                o.User.RequireUniqueEmail = false;
                o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<PersianIdentityErrorDescriber>()
            .AddPasswordValidator<ConfigurablePasswordValidator>()
            .AddSignInManager();

        return services;
    }
}
