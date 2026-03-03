using Microsoft.AspNetCore.Identity;

namespace InventoryApp.Models;

public class AppUser : IdentityUser<Guid>
{
    /// <summary>Display name chosen by the user at registration or via OAuth.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>True when the user has been granted admin privileges.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>ISO language code, e.g. "en" or "uz". Persisted per user.</summary>
    public string LanguagePreference { get; set; } = "en";

    /// <summary>"light" or "dark". Persisted per user.</summary>
    public string ThemePreference { get; set; } = "light";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>CDN URL from Cloudinary (or null). Never a local path.</summary>
    public string? AvatarUrl { get; set; }
}
