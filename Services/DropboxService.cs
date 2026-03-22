using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Services;

public class DropboxService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<DropboxService> _logger;

    public DropboxService(HttpClient httpClient, IConfiguration config, ILogger<DropboxService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a text/json file to Dropbox using the simple upload API.
    /// Needs a valid Access Token in appsettings: Dropbox:AccessToken.
    /// </summary>
    public async Task<bool> UploadJsonFileAsync(string folderPath, string fileName, object data)
    {
        var token = _config["Dropbox:AccessToken"];
        if (string.IsNullOrWhiteSpace(token) || token == "YOUR_DROPBOX_ACCESS_TOKEN")
        {
            _logger.LogError("Dropbox AccessToken is missing or not configured.");
            return false;
        }

        try
        {
            var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(jsonString));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload")
            {
                Content = fileContent
            };

            // Authentication
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Dropbox API args (JSON)
            var apiArgs = new
            {
                path = $"{folderPath}/{fileName}",
                mode = "add",
                autorename = true,
                mute = false,
                strict_conflict = false
            };
            request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(apiArgs));

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully uploaded {FileName} to Dropbox.", fileName);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Dropbox upload failed. Status: {Status}. Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while uploading file to Dropbox.");
            return false;
        }
    }
}
