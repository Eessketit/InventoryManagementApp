using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using InventoryApp.Models;

namespace InventoryApp.Pages;

public class SetLanguageModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public SetLanguageModel(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.LanguagePreference = culture;
                await _userManager.UpdateAsync(user);
            }
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}
