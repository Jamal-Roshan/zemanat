using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Zemanat.Core.Security;
using Zemanat.Core.Text;
using Zemanat.Data.Entities;

namespace Zemanat.Web.Auth;

/// <summary>Login and logout run as plain HTTP endpoints because the cookie can only be set outside the Blazor circuit.</summary>
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapPost("/account/login", async (HttpContext http, IFormCollection form, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            var userName = PersianText.ToLatinDigits(PersianText.CleanText(form["username"])).Replace(" ", string.Empty);
            var password = form["password"].ToString();
            var returnUrl = SafeReturnUrl(form["returnUrl"]);

            var user = await users.FindByNameAsync(userName);
            if (user is null || !user.IsActive)
            {
                // The same message for unknown and inactive users, so nothing leaks.
                return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                return Results.Redirect($"/login?error=locked&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            if (!result.Succeeded)
            {
                return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            return Results.LocalRedirect(user.MustChangePassword ? "/change-password" : returnUrl);
        });

        app.MapGet("/account/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.LocalRedirect("/login");
        });
    }

    /// <summary>Only local paths are allowed as a return address.</summary>
    public static string SafeReturnUrl(string? url)
        => !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\") ? url : "/";

    public static int? GetUserId(ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
