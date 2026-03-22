using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InventoryApp.Services;

namespace InventoryApp.Pages.Support;

[IgnoreAntiforgeryToken] // Allow AJAX POST without CSRF headache for this public endpoint
public class TicketModel : PageModel
{
    private readonly DropboxService _dropbox;
    private readonly IConfiguration _config;

    public TicketModel(DropboxService dropbox, IConfiguration config)
    {
        _dropbox = dropbox;
        _config = config;
    }

    [BindProperty]
    public TicketInput Input { get; set; } = new();

    public class TicketInput
    {
        public string Summary { get; set; } = "";
        public string Priority { get; set; } = "Low";
        public string Link { get; set; } = "";
        public string InventoryTitle { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Summary))
        {
            return BadRequest(new { success = false, message = "Summary is required" });
        }

        var userName = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Anonymous Guest";
        var admins = _config["Dropbox:AdminEmails"] ?? "admin@example.com";

        var payload = new
        {
            ReportedBy = userName,
            Inventory = string.IsNullOrWhiteSpace(Input.InventoryTitle) ? "N/A" : Input.InventoryTitle,
            Link = Input.Link,
            Priority = Input.Priority,
            Summary = Input.Summary,
            AdminEmails = admins,
            SubmittedAt = DateTime.UtcNow.ToString("O")
        };

        var fileName = $"Ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}.json";
        
        // Upload to a folder named SupportTickets in the Dropbox App folder
        var success = await _dropbox.UploadJsonFileAsync("/SupportTickets", fileName, payload);

        if (success)
        {
            return new JsonResult(new { success = true });
        }
        else
        {
            return StatusCode(500, new { success = false, message = "Failed to upload ticket to Dropbox." });
        }
    }
}
