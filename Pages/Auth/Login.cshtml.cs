using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using InventoryApp.Models;

namespace InventoryApp.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalProviders
        { get; set; } = [];

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "RememberMe")]
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

        // PasswordSignInAsync respects lockout (blocked) and isPersistent (Remember Me).
        // The custom AppUserClaimsPrincipalFactory automatically puts IsAdmin, Theme,
        // and DisplayName into the cookie — no manual claim wrangling needed here.
        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            isPersistent: Input.RememberMe,   // true → 30-day cookie; false → session only
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

        // Track last login time (fire-and-forget is fine here)
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return RedirectToPage("/Index");
    }

    /// <summary>Kicks off the external OAuth flow.</summary>
    public IActionResult OnPostExternalLogin(string provider)
    {
        var redirectUrl = Url.Page("/Auth/ExternalCallback");
        var properties  = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }
}
