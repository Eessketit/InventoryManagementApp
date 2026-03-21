using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using InventoryApp.Models;
using InventoryApp.Services;

namespace InventoryApp.Pages.Api;

/// <summary>
/// Accepts the "Sync to Salesforce" form and creates an Account + Contact
/// via SalesforceService. Only accessible by the account owner or an admin.
/// </summary>
[Authorize]
public class SalesforceModel : PageModel
{
    private readonly SalesforceService _sf;
    private readonly UserManager<AppUser> _userManager;

    public SalesforceModel(SalesforceService sf, UserManager<AppUser> userManager)
    {
        _sf          = sf;
        _userManager = userManager;
    }

    // Ignore this GET (it's an API-only endpoint)
    public IActionResult OnGet() => NotFound();

    public async Task<IActionResult> OnPostAsync(
        [FromForm] string targetUserId,
        [FromForm] string companyName,
        [FromForm] string? phone,
        [FromForm] string? website,
        [FromForm] string firstName,
        [FromForm] string lastName,
        [FromForm] string email,
        [FromForm] string? jobTitle)
    {
        // --- Permission check ---
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin  = User.FindFirst("IsAdmin")?.Value == "true";

        if (callerId != targetUserId && !isAdmin)
            return Forbid();

        // --- Basic validation ---
        if (string.IsNullOrWhiteSpace(companyName))
            return new JsonResult(new { success = false, error = "Company name is required." });

        if (string.IsNullOrWhiteSpace(lastName))
            return new JsonResult(new { success = false, error = "Last name is required." });

        try
        {
            var input = new SalesforceService.SalesforceInput(
                CompanyName: companyName.Trim(),
                Phone:       phone?.Trim(),
                Website:     website?.Trim(),
                FirstName:   firstName.Trim(),
                LastName:    lastName.Trim(),
                Email:       email.Trim(),
                JobTitle:    jobTitle?.Trim());

            var result = await _sf.CreateAccountAndContactAsync(input);

            return new JsonResult(new
            {
                success   = true,
                accountId = result.AccountId,
                contactId = result.ContactId,
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
