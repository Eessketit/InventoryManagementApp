using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InventoryManagementApp.Data;
using InventoryManagementApp.Models;

namespace InventoryManagementApp.Pages.Admin;

[Authorize]
public class UsersModel : PageModel
{
    private readonly AppDbContext _db;

    public UsersModel(AppDbContext db)
    {
        _db = db;
    }

    public List<User> Users { get; set; } = [];

    public async Task OnGetAsync()
    {
        Users = await _db.Users
            .OrderByDescending(u => u.LastLoginAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostBlockAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["StatusMessage"] = "Select at least one user to block.";
            TempData["StatusType"] = "warning";
            return RedirectToPage();
        }
        var users = await _db.Users
            .Where(u => selectedIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = UserStatus.Blocked;
        }

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Selected users were blocked.";
        TempData["StatusType"] = "success";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnblockAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["StatusMessage"] = "Select at least one user to unblock.";
            TempData["StatusType"] = "warning";
            return RedirectToPage();
        }
        var users = await _db.Users
            .Where(u => selectedIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = UserStatus.Active;
        }

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Selected users were unblocked.";
        TempData["StatusType"] = "success";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["StatusMessage"] = "Select at least one user to delete.";
            TempData["StatusType"] = "warning";
            return RedirectToPage();
        }

        var users = await _db.Users
            .Where(u => selectedIds.Contains(u.Id))
            .ToListAsync();

        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Selected users were deleted.";
        TempData["StatusType"] = "success";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteUnverifiedAsync(Guid[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            // status message for no-selection.
            TempData["StatusMessage"] = "Select at least one unverified user to delete.";
            TempData["StatusType"] = "warning";
            return RedirectToPage();
        }

        var users = await _db.Users
            .Where(u => selectedIds.Contains(u.Id) && u.Status == UserStatus.Unverified)
            .ToListAsync();

        if (users.Count == 0)
        {
            TempData["StatusMessage"] = "No unverified users were selected.";
            TempData["StatusType"] = "warning";
            return RedirectToPage();
        }

        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Selected unverified users were deleted.";
        TempData["StatusType"] = "success";

        return RedirectToPage();
    }
}
