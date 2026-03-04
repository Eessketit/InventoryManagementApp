using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.Pages.Inventories;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<InventoryRow> Inventories { get; set; } = [];
    public string? FilterTag { get; set; }
    public string? SearchQuery { get; set; }

    public record InventoryRow(
        Guid Id, string Title, string Description, string OwnerName,
        DateTime CreatedAt, int ItemCount, List<string> Tags);

    public async Task OnGetAsync(string? q, string? tag)
    {
        SearchQuery = q;
        FilterTag   = tag;

        var query = _db.Inventories
            .Include(i => i.Owner)
            .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(i => i.InventoryTags.Any(it => it.Tag.Name == tag));

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", q)));

        var raw = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id, i.Title, i.Description,
                OwnerName = i.Owner.Name,
                i.CreatedAt,
                ItemCount = i.Items.Count(),
                Tags = i.InventoryTags.Select(it => it.Tag.Name).ToList()
            })
            .ToListAsync();

        Inventories = raw.Select(r => new InventoryRow(
            r.Id, r.Title, r.Description, r.OwnerName,
            r.CreatedAt, r.ItemCount, r.Tags)).ToList();
    }
}
