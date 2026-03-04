using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InventoryApp.Models;

namespace InventoryApp.Pages.Api;

/// <summary>
/// Handles theme and language preference toggles from the navbar.
/// After updating the DB value, RefreshSignInAsync regenerates the
/// auth cookie via AppUserClaimsPrincipalFactory so the new Theme/Lang
/// claim is immediately visible on the next page load.
/// </summary>
[Authorize]
public class PreferencesModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public PreferencesModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
    }

    /// <summary>Toggle between "light" and "dark".</summary>
    public async Task<IActionResult> OnPostThemeAsync(string theme)
    {
        if (theme is not ("light" or "dark"))
            return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            user.ThemePreference = theme;
            await _userManager.UpdateAsync(user);

            // Regenerate the cookie so the new Theme claim is in the next request.
            await _signInManager.RefreshSignInAsync(user);
        }

        // Redirect back to wherever the user came from
        var returnUrl = Request.Headers.Referer.ToString();
        return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    /// <summary>Switch language ("en", "uz", etc.).</summary>
    public async Task<IActionResult> OnPostLanguageAsync(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            user.LanguagePreference = language;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
        }

        var returnUrl = Request.Headers.Referer.ToString();
        return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
