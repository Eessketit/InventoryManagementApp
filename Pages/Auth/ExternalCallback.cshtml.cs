using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using InventoryApp.Models;

namespace InventoryApp.Pages.Auth;

public class ExternalCallbackModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser>   _userManager;

    public ExternalCallbackModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
    }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return Page();
        }

        // Try signing in with the existing external login link
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);

        if (result.IsLockedOut)
        {
            ErrorMessage = "Your account has been blocked.";
            return Page();
        }

        if (result.Succeeded)
        {
            // Update last login
            var existing = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existing != null)
            {
                existing.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(existing);
            }
            return RedirectToPage("/Index");
        }

        // ── First-time OAuth user: auto-create local account ──────────────────
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "The external provider did not supply an email address.";
            return Page();
        }

        // Re-use existing local account if email already registered
        var user = await _userManager.FindByEmailAsync(email)
                   ?? BuildNewUser(info, email);

        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                ErrorMessage = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Page();
            }
        }

        await _userManager.AddLoginAsync(user, info);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // AppUserClaimsPrincipalFactory builds claims automatically
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Index");
    }

    private static AppUser BuildNewUser(ExternalLoginInfo info, string email)
    {
        var name = info.Principal.FindFirstValue(ClaimTypes.Name)
                   ?? email.Split('@')[0];

        return new AppUser
        {
            UserName       = email,
            Email          = email,
            Name           = name,
            EmailConfirmed = true,
            RegisteredAt   = DateTime.UtcNow,
        };
    }
}
