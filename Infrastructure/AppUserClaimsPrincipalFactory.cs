using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using InventoryApp.Models;

namespace InventoryApp.Infrastructure;

/// <summary>
/// Called by Identity every time it needs to build the ClaimsPrincipal
/// (on sign-in AND on cookie refresh). By overriding this we ensure
/// that IsAdmin, DisplayName, Theme and Lang are always baked into the
/// cookie from the AppUser entity — no manual claim management needed.
/// </summary>
public class AppUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<AppUser>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim("IsAdmin",     user.IsAdmin ? "true" : "false"));
        identity.AddClaim(new Claim("DisplayName", user.Name));
        identity.AddClaim(new Claim("Theme",       user.ThemePreference));
        identity.AddClaim(new Claim("Lang",        user.LanguagePreference));

        return identity;
    }
}
