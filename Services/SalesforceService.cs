using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InventoryApp.Services;

/// <summary>
/// Connects to Salesforce via OAuth 2.0 Username-Password flow
/// and creates Accounts + Contacts through the REST API.
/// </summary>
public class SalesforceService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SalesforceService> _logger;

    public SalesforceService(
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILogger<SalesforceService> logger)
    {
        _config     = config;
        _httpFactory = httpFactory;
        _logger     = logger;
    }

    /// <summary>Obtains an OAuth access token from Salesforce.</summary>
    private async Task<(string accessToken, string instanceUrl)> GetTokenAsync()
    {
        var loginUrl     = _config["Salesforce:LoginUrl"] ?? "https://login.salesforce.com";
        var clientId     = _config["Salesforce:ClientId"]     ?? throw new InvalidOperationException("Salesforce:ClientId not configured");
        var clientSecret = _config["Salesforce:ClientSecret"] ?? throw new InvalidOperationException("Salesforce:ClientSecret not configured");
        var username     = _config["Salesforce:Username"]     ?? throw new InvalidOperationException("Salesforce:Username not configured");
        var password     = _config["Salesforce:Password"]     ?? throw new InvalidOperationException("Salesforce:Password not configured");

        var client = _httpFactory.CreateClient();
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = clientId,
            ["client_secret"] = clientSecret,
            ["username"]      = username,
            ["password"]      = password,
        });

        var response = await client.PostAsync($"{loginUrl}/services/oauth2/token", body);
        var content  = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Salesforce token error: {Content}", content);
            throw new Exception($"Salesforce auth failed: {content}");
        }

        using var doc = JsonDocument.Parse(content);
        var token       = doc.RootElement.GetProperty("access_token").GetString()!;
        var instanceUrl = doc.RootElement.GetProperty("instance_url").GetString()!;

        return (token, instanceUrl);
    }

    /// <summary>Creates a Salesforce sObject and returns its new Id.</summary>
    private async Task<string> CreateSObjectAsync(
        HttpClient client,
        string instanceUrl,
        string objectType,
        object payload)
    {
        var json     = JsonSerializer.Serialize(payload);
        var request  = new HttpRequestMessage(
            HttpMethod.Post,
            $"{instanceUrl}/services/data/v60.0/sobjects/{objectType}");

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var content  = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Salesforce create {Object} error: {Content}", objectType, content);
            throw new Exception($"Salesforce create {objectType} failed: {content}");
        }

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    public record SalesforceInput(
        string CompanyName,
        string? Phone,
        string? Website,
        string  FirstName,
        string  LastName,
        string  Email,
        string? JobTitle);

    public record SalesforceResult(string AccountId, string ContactId);

    /// <summary>
    /// Creates an Account and a linked Contact in Salesforce.
    /// </summary>
    public async Task<SalesforceResult> CreateAccountAndContactAsync(SalesforceInput input)
    {
        var (token, instanceUrl) = await GetTokenAsync();

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // 1. Create Account
        var accountPayload = new Dictionary<string, object?> {
            ["Name"]    = input.CompanyName,
            ["Phone"]   = input.Phone,
            ["Website"] = input.Website,
        };
        var accountId = await CreateSObjectAsync(client, instanceUrl, "Account", accountPayload);

        // 2. Create Contact linked to the Account
        var contactPayload = new Dictionary<string, object?> {
            ["AccountId"] = accountId,
            ["FirstName"] = input.FirstName,
            ["LastName"]  = input.LastName,
            ["Email"]     = input.Email,
            ["Title"]     = input.JobTitle,
        };
        var contactId = await CreateSObjectAsync(client, instanceUrl, "Contact", contactPayload);

        _logger.LogInformation(
            "Created Salesforce Account {AccountId} and Contact {ContactId}",
            accountId, contactId);

        return new SalesforceResult(accountId, contactId);
    }
}
