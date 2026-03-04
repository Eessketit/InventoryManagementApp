namespace InventoryApp.Models;

/// <summary>
/// The element types that can be assembled into a custom ID format.
/// </summary>
public enum IdElementType
{
    FixedText   = 1,  // arbitrary Unicode text
    Random20Bit = 2,  // 20-bit random number (0–1048575)
    Random32Bit = 3,  // 32-bit random number (0–4294967295)
    Random6Digit = 4, // 6-digit random number (000000–999999)
    Random9Digit = 5, // 9-digit random number
    Guid        = 6,  // standard GUID
    DateTime    = 7,  // date/time at item creation
    Sequence    = 8   // max existing sequence + 1, per inventory
}
