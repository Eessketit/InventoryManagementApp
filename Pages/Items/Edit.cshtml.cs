using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Services;

namespace InventoryApp.Pages.Items;

[Authorize]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public ItemInput Input { get; set; } = new();

    public Inventory Inventory { get; set; } = null!;
    public List<InventoryField> ActiveFields { get; set; } = [];
    public bool IsEditing { get; set; }
    public string CustomIdPreview { get; set; } = "";

    public class ItemInput
    {
        public Guid? Id { get; set; }
        public Guid InventoryId { get; set; }

        public string? CustomId { get; set; } = "";
        
        public string? Text1 { get; set; }
        public string? Text2 { get; set; }
        public string? Text3 { get; set; }
        public string? MultiText1 { get; set; }
        public string? MultiText2 { get; set; }
        public string? MultiText3 { get; set; }
        public decimal? Number1 { get; set; }
        public decimal? Number2 { get; set; }
        public decimal? Number3 { get; set; }
        public string? Link1 { get; set; }
        public string? Link2 { get; set; }
        public string? Link3 { get; set; }
        public bool Bool1 { get; set; }
        public bool Bool2 { get; set; }
        public bool Bool3 { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid inventoryId, Guid? id)
    {
        var inv = await _db.Inventories
            .Include(i => i.AccessList)
            .Include(i => i.Fields.OrderBy(f => f.DisplayOrder))
            .Include(i => i.IdElements.OrderBy(e => e.DisplayOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == inventoryId);

        if (inv == null) return NotFound();
        if (!CheckCanWrite(inv)) return Forbid();

        Inventory = inv;
        ActiveFields = inv.Fields.ToList();

        if (id.HasValue && id.Value != Guid.Empty)
        {
            IsEditing = true;
            var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.InventoryId == inventoryId);
            if (item == null) return NotFound();

            Input = new ItemInput
            {
                Id = item.Id,
                InventoryId = item.InventoryId,
                CustomId = item.CustomId,
                Text1 = item.Text1, Text2 = item.Text2, Text3 = item.Text3,
                MultiText1 = item.MultiText1, MultiText2 = item.MultiText2, MultiText3 = item.MultiText3,
                Number1 = item.Number1, Number2 = item.Number2, Number3 = item.Number3,
                Link1 = item.Link1, Link2 = item.Link2, Link3 = item.Link3,
                Bool1 = item.Bool1, Bool2 = item.Bool2, Bool3 = item.Bool3
            };
        }
        else
        {
            IsEditing = false;
            Input.InventoryId = inventoryId;
            CustomIdPreview = CustomIdService.GetPreview(inv.IdElements);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var inv = await _db.Inventories
            .Include(i => i.AccessList)
            .Include(i => i.Fields.OrderBy(f => f.DisplayOrder))
            .Include(i => i.IdElements.OrderBy(e => e.DisplayOrder))
            .FirstOrDefaultAsync(i => i.Id == Input.InventoryId);
            
        if (inv == null) return NotFound();
        if (!CheckCanWrite(inv)) return Forbid();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Preload active fields to re-render properly if model is invalid
        Inventory = inv;
        ActiveFields = inv.Fields.ToList();

        if (!ModelState.IsValid)
            return Page();

        if (Input.Id.HasValue && Input.Id.Value != Guid.Empty)
        {
            // Edit
            var item = await _db.Items.FindAsync(Input.Id.Value);
            if (item == null || item.InventoryId != Input.InventoryId) return NotFound();

            if (!string.IsNullOrWhiteSpace(Input.CustomId))
                item.CustomId = Input.CustomId;
                
            AssignFields(item);
            item.UpdatedById = userId;
            item.UpdatedAt = DateTime.UtcNow;

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) {
                ModelState.AddModelError("Input.CustomId", "This ID is already taken (or another error occurred).");
                IsEditing = true;
                return Page();
            }
        }
        else
        {
            // Create
            var nextSeq = await _db.Items.Where(i => i.InventoryId == Input.InventoryId).CountAsync() + 1;
            string generatedId = CustomIdService.GenerateId(inv.IdElements, nextSeq);
            
            // Allow manual override
            string newId = string.IsNullOrWhiteSpace(Input.CustomId) ? generatedId : Input.CustomId!;

            var item = new Item
            {
                InventoryId = Input.InventoryId,
                CustomId = newId,
                CreatedById = userId
            };
            AssignFields(item);

            _db.Items.Add(item);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) {
                ModelState.AddModelError("Input.CustomId", "This Custom ID is already taken. Please try another.");
                IsEditing = false;
                CustomIdPreview = CustomIdService.GetPreview(inv.IdElements);
                return Page();
            }
        }

        return RedirectToPage("/Inventories/Detail", new { id = Input.InventoryId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid inventoryId, Guid id)
    {
        var inv = await _db.Inventories
            .Include(i => i.AccessList)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
            
        if (inv == null) return NotFound();
        if (!CheckCanWrite(inv)) return Forbid();

        var item = await _db.Items.FindAsync(id);
        if (item != null && item.InventoryId == inventoryId)
        {
            _db.Items.Remove(item);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Inventories/Detail", new { id = inventoryId });
    }

    // Helper functions
    private void AssignFields(Item item)
    {
        item.Text1 = Input.Text1; item.Text2 = Input.Text2; item.Text3 = Input.Text3;
        item.MultiText1 = Input.MultiText1; item.MultiText2 = Input.MultiText2; item.MultiText3 = Input.MultiText3;
        item.Number1 = Input.Number1; item.Number2 = Input.Number2; item.Number3 = Input.Number3;
        item.Link1 = Input.Link1; item.Link2 = Input.Link2; item.Link3 = Input.Link3;
        item.Bool1 = Input.Bool1; item.Bool2 = Input.Bool2; item.Bool3 = Input.Bool3;
    }

    private bool CheckCanWrite(Inventory inv)
    {
        var rawUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (rawUser == null) return false;
        var userId = Guid.Parse(rawUser);

        if (inv.OwnerId == userId) return true;
        if (User.FindFirst("IsAdmin")?.Value == "true") return true;
        if (inv.IsPublic) return true;
        if (inv.AccessList.Any(a => a.UserId == userId)) return true;
        
        return false;
    }
}
