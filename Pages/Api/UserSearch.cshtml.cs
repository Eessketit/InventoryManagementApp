using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;

namespace InventoryApp.Pages.Api;

/// <summary>
/// JSON endpoint: /Api/UserSearch?q=abc
/// Used by Tom Select autocomplete on the Access tab to find users by name or email.
/// Requires authentication — anonymous users cannot browse user accounts.
/// </summary>
public class UserSearchModel : PageModel
{
    private readonly AppDbContext _db;
    public UserSearchModel(AppDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(string? q)
    {
        if (!User.Identity!.IsAuthenticated)
            return new JsonResult(Array.Empty<object>());

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new JsonResult(Array.Empty<object>());

        var users = await _db.Users
            .Where(u => u.Name.Contains(q) || (u.Email != null && u.Email.Contains(q)))
            .OrderBy(u => u.Name)
            .Take(10)
            .Select(u => new { u.Id, u.Name, u.Email })
            .ToListAsync();

        return new JsonResult(users);
    }
}
