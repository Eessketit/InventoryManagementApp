namespace InventoryApp.Models;

/// <summary>
/// The five field types supported for custom inventory fields.
/// Each type has up to 3 slots (Slot 1, 2, 3) per inventory.
/// </summary>
public enum FieldType
{
    Text      = 1,   // single-line string
    MultiText = 2,   // multi-line string
    Number    = 3,   // decimal number
    Link      = 4,   // URL / document link (stored as string)
    Bool      = 5    // true/false checkbox
}
