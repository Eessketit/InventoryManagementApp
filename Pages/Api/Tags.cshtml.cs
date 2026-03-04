using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;

namespace InventoryApp.Pages.Api;

/// <summary>JSON endpoint: /Api/Tags?prefix=abc — used by Tagify autocomplete.</summary>
public class TagsModel : PageModel
{
    private readonly AppDbContext _db;
    public TagsModel(AppDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(string? prefix)
    {
        IQueryable<string> query = _db.Tags.Select(t => t.Name);

        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(n => n.StartsWith(prefix));

        var tags = await query.OrderBy(n => n).Take(20).ToListAsync();
        return new JsonResult(tags);
    }
}
