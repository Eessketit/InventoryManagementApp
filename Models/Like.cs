namespace InventoryApp.Models;

/// <summary>
/// One like by one user on one item.
/// Composite PK (ItemId, UserId) enforces the "one like per user per item" rule.
/// </summary>
public class Like
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
