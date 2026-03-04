using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<InventoryRow> Latest   { get; set; } = [];
    public List<InventoryRow> Popular  { get; set; } = [];
    public List<TagCount>     TagCloud { get; set; } = [];

    public record InventoryRow(Guid Id, string Title, string Description, string? ImageUrl, string OwnerName, int ItemCount);
    public record TagCount(string Name, int Count);

    public async Task OnGetAsync()
    {
        // Latest 10 inventories — single query
        Latest = await _db.Inventories
            .Include(i => i.Owner)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .Select(i => new InventoryRow(i.Id, i.Title, i.Description, i.ImageUrl,
                i.Owner.Name, i.Items.Count()))
            .ToListAsync();

        // Top 5 by item count — single query
        Popular = await _db.Inventories
            .Include(i => i.Owner)
            .AsNoTracking()
            .OrderByDescending(i => i.Items.Count())
            .Take(5)
            .Select(i => new InventoryRow(i.Id, i.Title, i.Description, i.ImageUrl,
                i.Owner.Name, i.Items.Count()))
            .ToListAsync();

        // Tag cloud — count of inventories per tag
        TagCloud = await _db.Tags
            .AsNoTracking()
            .Where(t => t.InventoryTags.Any())
            .Select(t => new TagCount(t.Name, t.InventoryTags.Count))
            .OrderByDescending(tc => tc.Count)
            .Take(40)
            .ToListAsync();
    }
}
