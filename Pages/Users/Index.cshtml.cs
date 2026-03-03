using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Models;

namespace InventoryApp.Pages.Users;

/// <summary>
/// Personal profile page — shows inventories the user owns
/// and inventories they have been granted write access to.
/// Phase 3 will populate the inventory tables; for now
/// this is a placeholder that confirms Identity is working.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(UserManager<AppUser> userManager)
        => _userManager = userManager;

    public AppUser? ProfileUser { get; set; }

    public async Task OnGetAsync()
    {
        ProfileUser = await _userManager.GetUserAsync(User);
    }
}
