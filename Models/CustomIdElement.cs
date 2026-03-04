namespace InventoryApp.Models;

/// <summary>
/// One element (building block) in a custom ID format for an inventory.
/// Elements are assembled in DisplayOrder to produce the custom ID string.
///
/// Config stores type-specific options as a simple JSON blob, e.g.:
///   FixedText  → { "text": "INV-" }
///   Random6Digit → { "leadingZeros": true }
///   DateTime   → { "format": "yyyyMMdd" }
/// </summary>
public class CustomIdElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;

    public IdElementType ElementType { get; set; }

    /// <summary>Position in the assembled ID string (0-based). Drag-and-drop reorders this.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// JSON blob with element-specific configuration.
    /// Kept simple; only a small, well-defined set of keys is ever written.
    /// </summary>
    public string Config { get; set; } = "{}";
}
