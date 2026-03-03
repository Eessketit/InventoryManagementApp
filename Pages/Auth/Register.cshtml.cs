using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InventoryApp.Models;

namespace InventoryApp.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public RegisterModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(2), Display(Name = "Display name")]
        public string Name { get; set; } = string.Empty;

        [Required, MinLength(6), Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            Name = Input.Name.Trim(),
            RegisteredAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        // Add app-level claims to the cookie directly
        await _userManager.AddClaimsAsync(user,
        [
            new Claim("DisplayName", user.Name),
            new Claim("IsAdmin", user.IsAdmin ? "true" : "false"),
            new Claim("Theme", user.ThemePreference),
            new Claim("Lang", user.LanguagePreference),
        ]);

        TempData["RegistrationSuccess"] = "Account created! Please sign in.";
        return RedirectToPage("/Auth/Login");
    }
}
