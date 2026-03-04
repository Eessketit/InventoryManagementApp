using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.Pages.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public AppUser?             ProfileUser  { get; set; }
    public List<InventoryRow>   OwnedInvs    { get; set; } = [];
    public List<InventoryRow>   AccessInvs   { get; set; } = [];

    public record InventoryRow(Guid Id, string Title, string? Category, int ItemCount, DateTime UpdatedAt);

    public async Task<IActionResult> OnGetAsync(Guid? userId)
    {
        // If no userId specified, show the current user's profile
        Guid targetId = userId ?? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        ProfileUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetId);
        if (ProfileUser == null) return NotFound();

        // Inventories owned by this user
        OwnedInvs = await _db.Inventories
            .Where(i => i.OwnerId == targetId)
            .AsNoTracking()
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new InventoryRow(i.Id, i.Title, i.Category, i.Items.Count(), i.UpdatedAt))
            .ToListAsync();

        // Inventories where this user has explicit write access
        AccessInvs = await _db.InventoryAccess
            .Where(a => a.UserId == targetId)
            .Select(a => new InventoryRow(
                a.InventoryId, a.Inventory.Title, a.Inventory.Category,
                a.Inventory.Items.Count(), a.Inventory.UpdatedAt))
            .AsNoTracking()
            .ToListAsync();

        return Page();
    }
}
