namespace InventoryApp.Models;

/// <summary>
/// Grants a specific user write-access (add/edit/delete items) to an inventory
/// that is NOT marked as public.  Only meaningful when Inventory.IsPublic = false.
/// </summary>
public class InventoryAccess
{
    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}
