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
        var latestQuery = await _db.Inventories
            .Include(i => i.Owner)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .Select(i => new { i.Id, i.Title, i.Description, i.ImageUrl, OwnerName = i.Owner.Name, ItemCount = i.Items.Count() })
            .ToListAsync();

        Latest = latestQuery
            .Select(i => new InventoryRow(i.Id, i.Title, i.Description, i.ImageUrl, i.OwnerName, i.ItemCount))
            .ToList();

        // Top 5 by item count — single query
        var popularQuery = await _db.Inventories
            .Include(i => i.Owner)
            .AsNoTracking()
            .OrderByDescending(i => i.Items.Count())
            .Take(5)
            .Select(i => new { i.Id, i.Title, i.Description, i.ImageUrl, OwnerName = i.Owner.Name, ItemCount = i.Items.Count() })
            .ToListAsync();

        Popular = popularQuery
            .Select(i => new InventoryRow(i.Id, i.Title, i.Description, i.ImageUrl, i.OwnerName, i.ItemCount))
            .ToList();

        // Tag cloud — count of inventories per tag
        var tagData = await _db.Tags
            .AsNoTracking()
            .Where(t => t.InventoryTags.Any())
            .Select(t => new { t.Name, Count = t.InventoryTags.Count })
            .OrderByDescending(x => x.Count)
            .Take(40)
            .ToListAsync();

        TagCloud = tagData
            .Select(x => new TagCount(x.Name, x.Count))
            .ToList();
    }
}
