namespace InventoryApp.Models;

public class Tag
{
    public int Id { get; set; }

    /// <summary>Tag name — unique, case-insensitive enforced by a DB index.</summary>
    public string Name { get; set; } = string.Empty;

    public ICollection<InventoryTag> InventoryTags { get; set; } = [];
}
