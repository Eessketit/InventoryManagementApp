using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InventoryApp.Models;

namespace InventoryApp.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>OAuth providers currently registered (shown as buttons).</summary>
    public IReadOnlyList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalProviders { get; set; } = [];

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync()
    {
        ExternalProviders = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ExternalProviders = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        // SignInManager checks lockout (blocked users) automatically
        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            isPersistent: Input.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked.");
            return Page();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        // Refresh app-level claims on every login so IsAdmin is always current
        await RefreshUserClaimsAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Kick off the OAuth flow: redirect to the external provider.
    /// </summary>
    public IActionResult OnPostExternalLogin(string provider)
    {
        var redirectUrl = Url.Page("/Auth/ExternalCallback");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task RefreshUserClaimsAsync(AppUser user)
    {
        // Remove stale app claims, then re-add fresh ones.
        var existing = await _userManager.GetClaimsAsync(user);
        var appClaims = existing
            .Where(c => c.Type is "DisplayName" or "IsAdmin" or "Theme" or "Lang")
            .ToList();
        if (appClaims.Count > 0)
            await _userManager.RemoveClaimsAsync(user, appClaims);

        await _userManager.AddClaimsAsync(user,
        [
            new Claim("DisplayName", user.Name),
            new Claim("IsAdmin", user.IsAdmin ? "true" : "false"),
            new Claim("Theme", user.ThemePreference),
            new Claim("Lang", user.LanguagePreference),
        ]);
    }
}
