namespace InventoryApp.Models;

/// <summary>
/// One item in an inventory.
///
/// Custom field values are stored in the fixed typed columns (Text1–Text3, etc.).
/// Which slots are active and what their labels are is determined by InventoryField rows.
///
/// The composite unique index (InventoryId, CustomId) is enforced at the DB level —
/// inventory-scoped IDs can duplicate across different inventories.
/// </summary>
public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;

    // ── Custom ID (inventory-scoped, user-editable string) ───────────────────
    /// <summary>
    /// Auto-generated on creation using the inventory's CustomIdElement list.
    /// Editable by users with write access, subject to format validation.
    /// Unique within its inventory (enforced by IX_items_inventory_custom).
    /// </summary>
    public string CustomId { get; set; } = string.Empty;

    // ── Fixed meta fields ─────────────────────────────────────────────────────
    public Guid CreatedById { get; set; }
    public AppUser CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedById { get; set; }
    public AppUser? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Custom field values — text (single-line) ──────────────────────────────
    public string? Text1 { get; set; }
    public string? Text2 { get; set; }
    public string? Text3 { get; set; }

    // ── Custom field values — multi-line text ─────────────────────────────────
    public string? MultiText1 { get; set; }
    public string? MultiText2 { get; set; }
    public string? MultiText3 { get; set; }

    // ── Custom field values — numeric ─────────────────────────────────────────
    public decimal? Number1 { get; set; }
    public decimal? Number2 { get; set; }
    public decimal? Number3 { get; set; }

    // ── Custom field values — document/image link ─────────────────────────────
    public string? Link1 { get; set; }
    public string? Link2 { get; set; }
    public string? Link3 { get; set; }

    // ── Custom field values — boolean ─────────────────────────────────────────
    public bool Bool1 { get; set; }
    public bool Bool2 { get; set; }
    public bool Bool3 { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<Like> Likes { get; set; } = [];
}
