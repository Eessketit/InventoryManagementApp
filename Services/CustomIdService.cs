using System.Text;
using System.Text.Json;
using InventoryApp.Models;

namespace InventoryApp.Services;

/// <summary>
/// Assembles a custom ID string from an inventory's CustomIdElement list.
/// All methods are static — no state needed.
/// </summary>
public static class CustomIdService
{
    /// <summary>
    /// Creates a real custom ID (with actual random values + sequence number).
    /// The caller must supply the next sequence value (MAX existing seq+1).
    /// </summary>
    public static string GenerateId(IEnumerable<CustomIdElement> elements, int nextSeq, DateTime? at = null)
    {
        var now = at ?? DateTime.UtcNow;
        var sb = new StringBuilder();
        foreach (var el in elements.OrderBy(e => e.DisplayOrder))
        {
            var cfg = JsonDocument.Parse(el.Config).RootElement;
            sb.Append(BuildSegment(el.ElementType, cfg, nextSeq, now));
        }
        return sb.ToString();
    }

    /// <summary>Returns a human-readable sample/preview of what the ID will look like.</summary>
    public static string GetPreview(IEnumerable<CustomIdElement> elements)
    {
        var sb = new StringBuilder();
        foreach (var el in elements.OrderBy(e => e.DisplayOrder))
        {
            var cfg = JsonDocument.Parse(el.Config).RootElement;
            sb.Append(PreviewSegment(el.ElementType, cfg));
        }
        return sb.Length == 0 ? "(empty)" : sb.ToString();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string BuildSegment(IdElementType type, JsonElement cfg, int seq, DateTime now)
        => type switch
        {
            IdElementType.FixedText    => GetStr(cfg, "text"),
            IdElementType.Random20Bit  => FormatNum(cfg, Random.Shared.Next(0, 1_048_576), 7),
            IdElementType.Random32Bit  => FormatNum(cfg, (long)uint.MaxValue & Random.Shared.NextInt64(), 10),
            IdElementType.Random6Digit => FormatNum(cfg, Random.Shared.Next(0, 1_000_000), 6),
            IdElementType.Random9Digit => FormatNum(cfg, Random.Shared.Next(0, 1_000_000_000), 9),
            IdElementType.Guid         => Guid.NewGuid().ToString(),
            IdElementType.DateTime     => now.ToString(GetStr(cfg, "format", "yyyyMMdd")),
            IdElementType.Sequence     => FormatNum(cfg, seq, 4),
            _                          => ""
        };

    private static string PreviewSegment(IdElementType type, JsonElement cfg)
        => type switch
        {
            IdElementType.FixedText    => GetStr(cfg, "text", "[text]"),
            IdElementType.Random20Bit  => FormatNum(cfg, 524288, 7),
            IdElementType.Random32Bit  => FormatNum(cfg, 2147483647L, 10),
            IdElementType.Random6Digit => FormatNum(cfg, 123456, 6),
            IdElementType.Random9Digit => FormatNum(cfg, 123456789, 9),
            IdElementType.Guid         => "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx",
            IdElementType.DateTime     => DateTime.UtcNow.ToString(GetStr(cfg, "format", "yyyyMMdd")),
            IdElementType.Sequence     => FormatNum(cfg, 1, 4),
            _                          => ""
        };

    private static string GetStr(JsonElement cfg, string key, string fallback = "")
        => cfg.TryGetProperty(key, out var v) ? v.GetString() ?? fallback : fallback;

    private static string FormatNum(JsonElement cfg, long value, int defaultWidth)
    {
        bool leadZero = cfg.TryGetProperty("leadingZeros", out var lz) && lz.ValueKind == JsonValueKind.True;
        int width = cfg.TryGetProperty("width", out var w) && w.TryGetInt32(out int wi) ? wi : defaultWidth;
        return leadZero ? value.ToString($"D{width}") : value.ToString();
    }
}
