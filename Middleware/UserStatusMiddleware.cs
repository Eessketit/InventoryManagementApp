using Microsoft.AspNetCore.Identity;
using InventoryApp.Models;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace InventoryApp.Middleware;

/// <summary>
/// After Identity authenticates the user, this middleware checks whether
/// the account is currently locked out (blocked by an admin).
/// If so, it signs them out and redirects to the login page.
/// This runs on every request so a block takes effect immediately
/// without waiting for the auth cookie to expire.
/// </summary>
public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        if (path.StartsWith("/auth/login")
            || path.StartsWith("/auth/register")
            || path.StartsWith("/auth/logout")
            || path.StartsWith("/auth/external")
            || path.StartsWith("/css")
            || path.StartsWith("/js")
            || path.StartsWith("/lib")
            || path.StartsWith("/_framework"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user == null || await userManager.IsLockedOutAsync(user))
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Auth/Login");
                return;
            }

            if (!context.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName) && 
                !string.IsNullOrEmpty(user.LanguagePreference))
            {
                var culture = new CultureInfo(user.LanguagePreference);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }

        await _next(context);
    }
}
