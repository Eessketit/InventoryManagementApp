using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InventoryApp.Models;

namespace InventoryApp.Pages.Admin;

[Authorize(Policy = "AdminOnly")]
public class UsersModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public UsersModel(UserManager<AppUser> userManager)
        => _userManager = userManager;

    public List<AppUser> Users { get; set; } = [];

    public async Task OnGetAsync()
    {
        Users = await _userManager.Users
            .OrderByDescending(u => u.LastLoginAt)
            .AsNoTracking()
            .ToListAsync();
    }

    // ── Block ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostBlockAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
            return SetStatus("Select at least one user to block.", "warning");

        foreach (var id in selectedIds)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) continue;
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        return SetStatus("Selected users were blocked.", "success");
    }

    // ── Unblock ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUnblockAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
            return SetStatus("Select at least one user to unblock.", "warning");

        foreach (var id in selectedIds)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) continue;
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        return SetStatus("Selected users were unblocked.", "success");
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
            return SetStatus("Select at least one user to delete.", "warning");

        foreach (var id in selectedIds)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
                await _userManager.DeleteAsync(user);
        }

        return SetStatus("Selected users were deleted.", "success");
    }

    // ── Make Admin ────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostMakeAdminAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
            return SetStatus("Select at least one user.", "warning");

        foreach (var id in selectedIds)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null || user.IsAdmin) continue;

            user.IsAdmin = true;
            await _userManager.UpdateAsync(user);
            await RefreshIsAdminClaim(user, isAdmin: true);
        }

        return SetStatus("Selected users were granted admin access.", "success");
    }

    // ── Remove Admin ──────────────────────────────────────────────────────────
    // NOTE: admins CAN remove themselves from admin (spec requirement).
    public async Task<IActionResult> OnPostRemoveAdminAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
            return SetStatus("Select at least one user.", "warning");

        foreach (var id in selectedIds)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null || !user.IsAdmin) continue;

            user.IsAdmin = false;
            await _userManager.UpdateAsync(user);
            await RefreshIsAdminClaim(user, isAdmin: false);
        }

        // If the current admin removed their own access, sign them out of admin view
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != null && selectedIds.Any(id => id.ToString() == currentUserId))
            return RedirectToPage("/Index");

        return SetStatus("Admin access removed for selected users.", "success");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task RefreshIsAdminClaim(AppUser user, bool isAdmin)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var old = claims.FirstOrDefault(c => c.Type == "IsAdmin");
        if (old != null)
            await _userManager.RemoveClaimAsync(user, old);
        await _userManager.AddClaimAsync(user, new Claim("IsAdmin", isAdmin ? "true" : "false"));
    }

    private IActionResult SetStatus(string message, string type)
    {
        TempData["StatusMessage"] = message;
        TempData["StatusType"] = type;
        return RedirectToPage();
    }
}
