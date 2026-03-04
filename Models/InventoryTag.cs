namespace InventoryApp.Models;

/// <summary>Join table linking Inventories ↔ Tags (many-to-many).</summary>
public class InventoryTag
{
    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
