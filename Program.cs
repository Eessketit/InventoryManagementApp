using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using InventoryApp.Data;
using InventoryApp.Middleware;
using InventoryApp.Models;

var builder = WebApplication.CreateBuilder(args);

// ── KESTREL (Render reads PORT env var) ─────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// ── DATABASE ────────────────────────────────────────────────────────────────
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var dbPort = uri.Port > 0 ? uri.Port : 5432;

    if (userInfo.Length != 2)
        throw new InvalidOperationException("DATABASE_URL is missing username/password credentials.");

    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = Uri.UnescapeDataString(userInfo[1]);
    var databaseName = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

    connectionString =
        $"Host={uri.Host};" +
        $"Port={dbPort};" +
        $"Database={databaseName};" +
        $"Username={username};" +
        $"Password={password};" +
        "SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No database connection string configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── IDENTITY ────────────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
    {
        // Relax password rules slightly for a course project
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;

        // No email confirmation required (simplifies the flow)
        options.SignIn.RequireConfirmedEmail = false;

        // Lockout: after 5 bad attempts, lock for 15 min
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Configure the Identity cookie so auth redirects go to our pages
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── OAUTH ────────────────────────────────────────────────────────────────────
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
var fbAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

// Only wire up providers when credentials are present —
// keeps the dev environment working without secrets configured.
var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        // Request name + avatar from Google
        options.Scope.Add("profile");
        options.SaveTokens = false;
    });
}

if (!string.IsNullOrWhiteSpace(fbAppId) && !string.IsNullOrWhiteSpace(fbAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = fbAppId;
        options.AppSecret = fbAppSecret;
        options.Fields.Add("name");
        options.Fields.Add("picture");
    });
}

// ── AUTHORIZATION ────────────────────────────────────────────────────────────
// AdminOnly policy: user must have the IsAdmin claim set to "true".
// This claim is baked into the cookie at login time.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});

builder.Services.AddRazorPages();

// ── BUILD ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── AUTO MIGRATIONS ──────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// HTTPS redirect only in production (Render handles TLS at the edge)
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<UserStatusMiddleware>();
app.UseAuthorization();

app.MapRazorPages();
app.Run();
