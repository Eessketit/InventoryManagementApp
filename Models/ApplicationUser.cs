using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public bool IsBlocked { get; set; }
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
}