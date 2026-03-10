using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using InventoryApp.Models;

namespace InventoryApp.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public RegisterModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalProviders
        { get; set; } = [];

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(2), Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required, MinLength(6), DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
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

        var user = new AppUser
        {
            Id           = Guid.NewGuid(),
            UserName     = Input.Email.Trim(),
            Email        = Input.Email.Trim(),
            Name         = Input.Name.Trim(),
            RegisteredAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Users/Index");
    }

    /// <summary>Kicks off the external OAuth flow.</summary>
    public IActionResult OnPostExternalLogin(string provider)
    {
        var redirectUrl = Url.Page("/Auth/ExternalCallback");
        var properties  = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }
}
