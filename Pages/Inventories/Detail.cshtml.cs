using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Services;

namespace InventoryApp.Pages.Inventories;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly MarkdownService _md;
    public DetailModel(AppDbContext db, MarkdownService md) { _db = db; _md = md; }

    // ── Loaded data ───────────────────────────────────────────────────────────
    public Inventory Inventory { get; set; } = null!;
    public List<InventoryField>  Fields     { get; set; } = [];
    public List<CustomIdElement> IdElements { get; set; } = [];
    public List<Comment>         Comments   { get; set; } = [];
    public List<ItemRow>         Items      { get; set; } = [];
    public HashSet<Guid>         LikedIds   { get; set; } = [];

    // ── Access ────────────────────────────────────────────────────────────────
    public bool IsOwner  { get; private set; }
    public bool IsAdmin  { get; private set; }
    public bool CanEdit  { get; private set; }  // owner or admin
    public bool CanWrite { get; private set; }  // may add/edit/delete items

    // ── Computed for view ─────────────────────────────────────────────────────
    public string TagsCsv      { get; set; } = "";
    public string DescHtml     { get; set; } = "";
    public string ActiveTab    { get; set; } = "items";
    public InventoryStats Stats { get; set; } = new();

    // ── Input models ──────────────────────────────────────────────────────────
    public class SettingsInput
    {
        public string  Title       { get; set; } = "";
        public string  Description { get; set; } = "";
        public string? Category    { get; set; }
        public string? ImageUrl    { get; set; }
        public bool    IsPublic    { get; set; }
        public string  Tags        { get; set; } = ""; // comma-separated
    }

    public class FieldUpdate
    {
        public Guid   FieldId     { get; set; }
        public string Title       { get; set; } = "";
        public string Description { get; set; } = "";
        public bool   ShowInTable { get; set; }
        public int    DisplayOrder{ get; set; }
    }

    public class IdElementInput
    {
        public IdElementType ElementType { get; set; }
        public string        Config      { get; set; } = "{}";
    }

    public record ItemRow(
        Guid Id, string CustomId, string CreatedByName, DateTime CreatedAt,
        int LikeCount, string?[] Values);

    public class InventoryStats
    {
        public int ItemCount { get; set; }
        // For each numeric slot: min/max/avg; null if field inactive
        public NumStat?[] Numbers { get; set; } = new NumStat?[3];
        // For each text slot: top-5 values; null if field inactive
        public List<string>?[] TopTexts { get; set; } = new List<string>?[3];
    }

    public record NumStat(decimal Min, decimal Max, decimal Avg);

    // ─────────────────────────────────────────────────────────────────────────
    // GET
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(Guid id, string? tab)
    {
        ActiveTab = tab ?? "items";
        return await LoadAsync(id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Settings auto-save (AJAX, returns JSON)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAutoSaveAsync(Guid id,
        [FromForm] SettingsInput settings)
    {
        if (!User.Identity!.IsAuthenticated)
            return new JsonResult(new { success = false, message = "Not authenticated" });

        var inventory = await _db.Inventories
            .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inventory == null) return new JsonResult(new { success = false, message = "Not found" });
        if (!CheckCanEdit(inventory)) return new JsonResult(new { success = false, message = "Access denied" });

        inventory.Title       = settings.Title.Trim().Length > 0 ? settings.Title.Trim() : inventory.Title;
        inventory.Description = settings.Description;
        inventory.Category    = string.IsNullOrWhiteSpace(settings.Category) ? null : settings.Category;
        inventory.ImageUrl    = string.IsNullOrWhiteSpace(settings.ImageUrl)  ? null : settings.ImageUrl;
        inventory.IsPublic    = settings.IsPublic;
        inventory.UpdatedAt   = DateTime.UtcNow;

        await UpdateTagsAsync(inventory, settings.Tags);

        try
        {
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return new JsonResult(new { success = false, message = "Conflict — another user just saved. Please refresh." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Fields
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddFieldAsync(Guid id, FieldType type)
    {
        if (!await RequireEditAsync(id)) return Forbid();

        var usedSlots = await _db.InventoryFields
            .Where(f => f.InventoryId == id && f.Type == type)
            .Select(f => f.Slot).ToListAsync();

        int slot = Enumerable.Range(1, 3).Except(usedSlots).FirstOrDefault();
        if (slot == 0)
        {
            TempData["Error"] = $"Maximum of 3 fields of type {type} already added.";
            return RedirectToPage(new { id, tab = "fields" });
        }

        var maxOrderQuery = _db.InventoryFields
            .Where(f => f.InventoryId == id);
            
        int maxOrder = 0;
        if (await maxOrderQuery.AnyAsync())
        {
            maxOrder = await maxOrderQuery.MaxAsync(f => f.DisplayOrder);
        }

        _db.InventoryFields.Add(new InventoryField
        {
            InventoryId  = id,
            Type         = type,
            Slot         = slot,
            Title        = $"New {type} field",
            ShowInTable  = true,
            DisplayOrder = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id, tab = "fields" });
    }

    public async Task<IActionResult> OnPostSaveFieldsAsync(Guid id,
        List<FieldUpdate>? fieldUpdates)
    {
        if (!await RequireEditAsync(id)) return Forbid();

        if (fieldUpdates == null || fieldUpdates.Count == 0)
            return RedirectToPage(new { id, tab = "fields" });

        var fieldIds = fieldUpdates.Select(f => f.FieldId).ToList();
        var fields = await _db.InventoryFields
            .Where(f => f.InventoryId == id && fieldIds.Contains(f.Id))
            .ToListAsync();

        foreach (var upd in fieldUpdates)
        {
            var f = fields.FirstOrDefault(x => x.Id == upd.FieldId);
            if (f == null) continue;
            var trimmedTitle = upd.Title?.Trim() ?? "";
            f.Title        = trimmedTitle.Length > 0 ? trimmedTitle : f.Title;
            f.Description  = upd.Description?.Trim() ?? "";
            f.ShowInTable  = upd.ShowInTable;
            f.DisplayOrder = upd.DisplayOrder;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id, tab = "fields" });
    }

    public async Task<IActionResult> OnPostDeleteFieldsAsync(Guid id, Guid[] selectedFieldIds)
    {
        if (!await RequireEditAsync(id)) return Forbid();

        var fields = await _db.InventoryFields
            .Where(f => f.InventoryId == id && selectedFieldIds.Contains(f.Id))
            .ToListAsync();

        _db.InventoryFields.RemoveRange(fields);
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id, tab = "fields" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Access
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostSaveAccessAsync(Guid id,
        bool isPublic, Guid[]? addUserIds, Guid[]? removeUserIds)
    {
        var inventory = await _db.Inventories.FindAsync(id);
        if (inventory == null || !CheckCanEdit(inventory)) return Forbid();

        inventory.IsPublic = isPublic;

        if (removeUserIds?.Length > 0)
        {
            var toRemove = await _db.InventoryAccess
                .Where(a => a.InventoryId == id && removeUserIds.Contains(a.UserId))
                .ToListAsync();
            _db.InventoryAccess.RemoveRange(toRemove);
        }

        if (addUserIds?.Length > 0)
        {
            var existing = await _db.InventoryAccess
                .Where(a => a.InventoryId == id)
                .Select(a => a.UserId).ToListAsync();

            foreach (var uid in addUserIds.Except(existing))
                _db.InventoryAccess.Add(new InventoryAccess { InventoryId = id, UserId = uid });
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id, tab = "access" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Custom ID elements
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostSaveCustomIdAsync(Guid id,
        List<IdElementInput> elements)
    {
        if (!await RequireEditAsync(id)) return Forbid();

        var existing = await _db.CustomIdElements.Where(e => e.InventoryId == id).ToListAsync();
        _db.CustomIdElements.RemoveRange(existing);

        if (elements == null) return RedirectToPage(new { id, tab = "customid" });

        for (int i = 0; i < elements.Count && i < 10; i++)
        {
            _db.CustomIdElements.Add(new CustomIdElement
            {
                InventoryId  = id,
                ElementType  = elements[i].ElementType,
                DisplayOrder = i,
                Config       = elements[i].Config
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id, tab = "customid" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Discussion comment
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddCommentAsync(Guid id, string body)
    {
        if (!User.Identity!.IsAuthenticated) return Forbid();
        if (string.IsNullOrWhiteSpace(body)) return RedirectToPage(new { id, tab = "discussion" });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _db.Comments.Add(new Comment
        {
            InventoryId = id,
            AuthorId    = userId,
            Body        = body.Trim()
        });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id, tab = "discussion" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Delete inventory
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteInventoryAsync(Guid id)
    {
        var inventory = await _db.Inventories.FindAsync(id);
        if (inventory == null) return NotFound();
        if (!CheckCanEdit(inventory)) return Forbid();

        _db.Inventories.Remove(inventory);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Inventories/Index");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Toggle Like (AJAX, returns JSON)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostToggleLikeAsync(Guid id, Guid itemId)
    {
        if (!User.Identity!.IsAuthenticated)
            return new JsonResult(new { success = false, message = "Not authenticated" });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var existing = await _db.Likes.FirstOrDefaultAsync(l => l.ItemId == itemId && l.UserId == userId);
        bool liked;

        if (existing != null)
        {
            _db.Likes.Remove(existing);
            liked = false;
        }
        else
        {
            _db.Likes.Add(new Like { ItemId = itemId, UserId = userId });
            liked = true;
        }

        await _db.SaveChangesAsync();

        var newCount = await _db.Likes.CountAsync(l => l.ItemId == itemId);
        return new JsonResult(new { success = true, liked, newCount });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Preview custom ID (AJAX, returns JSON)
    // ─────────────────────────────────────────────────────────────────────────
    public IActionResult OnPostPreviewCustomId(List<IdElementInput> elements)
    {
        var dummyElements = elements.Select((e, i) => new CustomIdElement
        {
            ElementType  = e.ElementType,
            DisplayOrder = i,
            Config       = e.Config
        });
        var preview = CustomIdService.GetPreview(dummyElements);
        return new JsonResult(new { preview });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<IActionResult> LoadAsync(Guid id)
    {
        var inventory = await _db.Inventories
            .Include(i => i.Owner)
            .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
            .Include(i => i.Fields.OrderBy(f => f.DisplayOrder))
            .Include(i => i.AccessList).ThenInclude(a => a.User)
            .Include(i => i.IdElements.OrderBy(e => e.DisplayOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inventory == null) return NotFound();

        Inventory  = inventory;
        Fields     = inventory.Fields.ToList();
        IdElements = inventory.IdElements.ToList();

        // Access flags
        var userId = GetUserId();
        IsAdmin  = User.FindFirst("IsAdmin")?.Value == "true";
        IsOwner  = userId.HasValue && inventory.OwnerId == userId.Value;
        var hasWriteAccess = userId.HasValue &&
            inventory.AccessList.Any(a => a.UserId == userId.Value);
        CanEdit  = IsOwner || IsAdmin;
        CanWrite = CanEdit
            || (User.Identity!.IsAuthenticated && inventory.IsPublic)
            || hasWriteAccess;

        // Computed
        TagsCsv  = string.Join(",", inventory.InventoryTags.Select(it => it.Tag.Name));
        DescHtml = _md.ToHtml(inventory.Description);

        // Comments (for discussion tab)
        Comments = await _db.Comments
            .Where(c => c.InventoryId == id)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        // Items table (for items tab) — single query via projection
        var rawItems = await _db.Items
            .Where(i => i.InventoryId == id)
            .Include(i => i.CreatedBy)
            .Include(i => i.Likes)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        // Collect current user's liked item IDs
        if (userId.HasValue)
        {
            LikedIds = rawItems
                .Where(i => i.Likes.Any(l => l.UserId == userId.Value))
                .Select(i => i.Id)
                .ToHashSet();
        }

        // Build item rows with slot values ordered by field DisplayOrder
        Items = rawItems.Select(item => new ItemRow(
            item.Id, item.CustomId, item.CreatedBy.Name, item.CreatedAt,
            item.Likes.Count,
            GetSlotValues(item)
        )).ToList();

        // Stats
        await ComputeStatsAsync(id);

        return Page();
    }

    private string?[] GetSlotValues(Item item)
    {
        // Returns values for active visible fields in display order
        var result = new List<string?>();
        foreach (var f in Fields.Where(f => f.ShowInTable))
        {
            result.Add(f.Type switch
            {
                FieldType.Text      => f.Slot == 1 ? item.Text1 : f.Slot == 2 ? item.Text2 : item.Text3,
                FieldType.MultiText => f.Slot == 1 ? item.MultiText1 : f.Slot == 2 ? item.MultiText2 : item.MultiText3,
                FieldType.Number    => f.Slot == 1 ? item.Number1?.ToString() : f.Slot == 2 ? item.Number2?.ToString() : item.Number3?.ToString(),
                FieldType.Link      => f.Slot == 1 ? item.Link1 : f.Slot == 2 ? item.Link2 : item.Link3,
                FieldType.Bool      => f.Slot == 1 ? (item.Bool1 ? "✓" : "✗") : f.Slot == 2 ? (item.Bool2 ? "✓" : "✗") : (item.Bool3 ? "✓" : "✗"),
                _                   => null
            });
        }
        return result.ToArray();
    }

    private async Task ComputeStatsAsync(Guid id)
    {
        var values = await _db.Items.Where(i => i.InventoryId == id)
            .Select(i => new
            {
                i.Number1, i.Number2, i.Number3,
                i.Text1,   i.Text2,   i.Text3
            })
            .ToListAsync();

        Stats.ItemCount = values.Count;

        var numFields = Fields.Where(f => f.Type == FieldType.Number).ToList();
        for (int s = 1; s <= 3; s++)
        {
            if (!numFields.Any(f => f.Slot == s)) continue;
            var nums = s switch
            {
                1 => values.Where(v => v.Number1.HasValue).Select(v => v.Number1!.Value).ToList(),
                2 => values.Where(v => v.Number2.HasValue).Select(v => v.Number2!.Value).ToList(),
                _ => values.Where(v => v.Number3.HasValue).Select(v => v.Number3!.Value).ToList()
            };
            if (nums.Count > 0)
                Stats.Numbers[s - 1] = new NumStat(nums.Min(), nums.Max(), nums.Average());
        }

        var textFields = Fields.Where(f => f.Type == FieldType.Text).ToList();
        for (int s = 1; s <= 3; s++)
        {
            if (!textFields.Any(f => f.Slot == s)) continue;
            var texts = s switch
            {
                1 => values.Where(v => v.Text1 != null).Select(v => v.Text1!).ToList(),
                2 => values.Where(v => v.Text2 != null).Select(v => v.Text2!).ToList(),
                _ => values.Where(v => v.Text3 != null).Select(v => v.Text3!).ToList()
            };
            Stats.TopTexts[s - 1] = texts
                .GroupBy(t => t).OrderByDescending(g => g.Count())
                .Take(5).Select(g => g.Key).ToList();
        }
    }

    private async Task UpdateTagsAsync(Inventory inventory, string tagsCsv)
    {
        var desired = tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant()).Distinct().ToList();

        // Remove old links
        var oldLinks = inventory.InventoryTags.ToList();
        _db.InventoryTags.RemoveRange(oldLinks);

        // Get or create tag entities (no query inside loop — batch it)
        var existingTags = await _db.Tags
            .Where(t => desired.Contains(t.Name))
            .ToListAsync();

        foreach (var name in desired)
        {
            var tag = existingTags.FirstOrDefault(t => t.Name == name)
                      ?? new Tag { Name = name };

            if (tag.Id == 0) _db.Tags.Add(tag); // new tag

            _db.InventoryTags.Add(new InventoryTag { Inventory = inventory, Tag = tag });
        }
    }

    private bool CheckCanEdit(Inventory inv)
    {
        var userId = GetUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "true";
        return (userId.HasValue && inv.OwnerId == userId.Value) || isAdmin;
    }

    private async Task<bool> RequireEditAsync(Guid invId)
    {
        if (!User.Identity!.IsAuthenticated) return false;
        var inv = await _db.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invId);
        return inv != null && CheckCanEdit(inv);
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return raw == null ? null : Guid.Parse(raw);
    }

    public static readonly string[] Categories =
        ["Equipment", "Furniture", "Book", "Document", "Other"];
}
