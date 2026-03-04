namespace InventoryApp.Models;

/// <summary>
/// Defines one custom field for all items in an inventory.
///
/// IMPORTANT — fixed-slot design:
///   Items have pre-allocated columns Text1…Text3, Number1…Number3, etc.
///   This entity only configures whether a slot is "on", its label, and display position.
///   Max 3 fields per FieldType (Slot is 1, 2, or 3).
/// </summary>
public class InventoryField
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;

    /// <summary>Field type: Text, MultiText, Number, Link, or Bool.</summary>
    public FieldType Type { get; set; }

    /// <summary>Column slot within the type: 1, 2, or 3.</summary>
    public int Slot { get; set; }

    /// <summary>User-defined label shown on forms and table headers.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Hint shown as a tooltip/description on item forms.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether this field's column appears in the inventory items table.</summary>
    public bool ShowInTable { get; set; } = true;

    /// <summary>Display order among all fields of this inventory (drag-and-drop).</summary>
    public int DisplayOrder { get; set; }
}
