namespace InventoryApp.Models;

public class Inventory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display title of the inventory.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown-formatted description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Category chosen from a predefined list (values managed in DB, no UI needed).</summary>
    public string? Category { get; set; }

    /// <summary>Cloudinary CDN URL.  Never a local path.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>When true any authenticated user may add/edit/delete items.</summary>
    public bool IsPublic { get; set; }

    // ── Ownership ────────────────────────────────────────────────────────────
    public Guid OwnerId { get; set; }
    public AppUser Owner { get; set; } = null!;

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<InventoryTag>    InventoryTags    { get; set; } = [];
    public ICollection<InventoryField>  Fields           { get; set; } = [];
    public ICollection<InventoryAccess> AccessList       { get; set; } = [];
    public ICollection<Item>            Items            { get; set; } = [];
    public ICollection<CustomIdElement> IdElements       { get; set; } = [];
    public ICollection<Comment>         Comments         { get; set; } = [];

    // ── Search ────────────────────────────────────────────────────────────────
    public NpgsqlTypes.NpgsqlTsVector SearchVector { get; set; } = null!;
}
