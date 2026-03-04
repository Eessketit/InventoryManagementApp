namespace InventoryApp.Models;

/// <summary>
/// A linear discussion post on an inventory page.
/// Posts are always appended; no threading.
/// Body is stored as Markdown and rendered server-side.
/// </summary>
public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;

    /// <summary>Markdown text. Rendered to HTML via Markdig when displayed.</summary>
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
