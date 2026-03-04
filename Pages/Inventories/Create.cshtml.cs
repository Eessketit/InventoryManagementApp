using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.Pages.Inventories;

[Authorize]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty, Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? Category { get; set; }

    [BindProperty]
    public bool IsPublic { get; set; }

    // List of available categories (managed in DB; no UI to add them)
    public static readonly string[] Categories =
        ["Equipment", "Furniture", "Book", "Document", "Other"];

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var inventory = new Inventory
        {
            Title       = Title.Trim(),
            Description = Description,
            Category    = string.IsNullOrWhiteSpace(Category) ? null : Category,
            IsPublic    = IsPublic,
            OwnerId     = userId,
        };

        _db.Inventories.Add(inventory);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Inventories/Detail", new { id = inventory.Id });
    }
}
