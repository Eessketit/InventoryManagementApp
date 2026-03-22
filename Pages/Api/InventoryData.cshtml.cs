using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.Pages.Api;

/// <summary>
/// Public read-only JSON API endpoint.
/// GET /Api/InventoryData?token={token}
/// Returns aggregated inventory data; no authentication required (token is the credential).
/// </summary>
public class InventoryDataModel : PageModel
{
    private readonly AppDbContext _db;
    public InventoryDataModel(AppDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new JsonResult(new { error = "Missing token" }) { StatusCode = 400 };

        var inventory = await _db.Inventories
            .Include(i => i.Fields)
            .FirstOrDefaultAsync(i => i.ApiToken == token.Trim());

        if (inventory == null)
            return new JsonResult(new { error = "Inventory not found or invalid token" }) { StatusCode = 404 };

        // Load items for this inventory
        var items = await _db.Items
            .Where(i => i.InventoryId == inventory.Id)
            .ToListAsync();

        // Build field stats
        var fieldStats = new List<object>();
        foreach (var field in inventory.Fields.OrderBy(f => f.DisplayOrder))
        {
            object stats = field.Type switch
            {
                FieldType.Number => BuildNumberStats(items, field.Slot),
                FieldType.Text or FieldType.MultiText => BuildTextStats(items, field.Type, field.Slot),
                FieldType.Bool => BuildBoolStats(items, field.Slot),
                FieldType.Link => BuildLinkStats(items, field.Slot),
                _ => new { }
            };

            fieldStats.Add(new
            {
                title    = field.Title,
                type     = field.Type.ToString(),
                slot     = field.Slot,
                inTable  = field.ShowInTable,
                stats
            });
        }

        var result = new
        {
            title       = inventory.Title,
            description = inventory.Description,
            category    = inventory.Category,
            isPublic    = inventory.IsPublic,
            createdAt   = inventory.CreatedAt,
            updatedAt   = inventory.UpdatedAt,
            itemCount   = items.Count,
            fields      = fieldStats
        };

        return new JsonResult(result);
    }

    // ── Aggregation helpers ──────────────────────────────────────────────────

    private static object BuildNumberStats(List<Item> items, int slot)
    {
        var values = slot switch
        {
            1 => items.Where(i => i.Number1.HasValue).Select(i => i.Number1!.Value).ToList(),
            2 => items.Where(i => i.Number2.HasValue).Select(i => i.Number2!.Value).ToList(),
            3 => items.Where(i => i.Number3.HasValue).Select(i => i.Number3!.Value).ToList(),
            _ => []
        };

        if (values.Count == 0)
            return new { count = 0 };

        return new
        {
            count = values.Count,
            min   = values.Min(),
            max   = values.Max(),
            avg   = Math.Round(values.Average(), 4)
        };
    }

    private static object BuildTextStats(List<Item> items, FieldType type, int slot)
    {
        IEnumerable<string?> rawValues = (type, slot) switch
        {
            (FieldType.Text, 1) => items.Select(i => i.Text1),
            (FieldType.Text, 2) => items.Select(i => i.Text2),
            (FieldType.Text, 3) => items.Select(i => i.Text3),
            (FieldType.MultiText, 1) => items.Select(i => i.MultiText1),
            (FieldType.MultiText, 2) => items.Select(i => i.MultiText2),
            (FieldType.MultiText, 3) => items.Select(i => i.MultiText3),
            _ => []
        };

        var top5 = rawValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { value = g.Key, count = g.Count() })
            .ToList();

        return new { top5 };
    }

    private static object BuildBoolStats(List<Item> items, int slot)
    {
        var values = slot switch
        {
            1 => items.Select(i => i.Bool1).ToList(),
            2 => items.Select(i => i.Bool2).ToList(),
            3 => items.Select(i => i.Bool3).ToList(),
            _ => []
        };

        return new
        {
            trueCount  = values.Count(v => v),
            falseCount = values.Count(v => !v),
            total      = values.Count
        };
    }

    private static object BuildLinkStats(List<Item> items, int slot)
    {
        var values = slot switch
        {
            1 => items.Select(i => i.Link1).ToList(),
            2 => items.Select(i => i.Link2).ToList(),
            3 => items.Select(i => i.Link3).ToList(),
            _ => []
        };

        return new
        {
            notEmptyCount = values.Count(v => !string.IsNullOrWhiteSpace(v)),
            total         = values.Count
        };
    }
}
