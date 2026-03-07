using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;
using InventoryApp.Infrastructure;
using InventoryApp.Middleware;
using InventoryApp.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ── KESTREL ──────────────────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// ── DATABASE ─────────────────────────────────────────────────────────────────
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var dbPort = uri.Port > 0 ? uri.Port : 5432;

    connectionString =
        $"Host={uri.Host};" +
        $"Port={dbPort};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"Username={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        "SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No database connection string configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── IDENTITY ──────────────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── CUSTOM CLAIMS FACTORY ────────────────────────────────────────────────────
// Replaces the default factory so IsAdmin/DisplayName/Theme/Lang are always
// included in the cookie, read directly from the AppUser entity.
builder.Services
    .AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();

// ── COOKIE SETTINGS ───────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    // Fix for "Remember Me": when isPersistent=false the cookie is session-only.
    // When isPersistent=true (Remember Me checked) it lives for 30 days.
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── OAUTH ─────────────────────────────────────────────────────────────────────
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.Scope.Add("profile");
    });

if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret))
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
        options.Scope.Add("user:email");
    });

// ── AUTHORIZATION ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true")));

builder.Services.AddSingleton<InventoryApp.Services.MarkdownService>();

builder.Services.AddRazorPages();

// ── BUILD ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── MIGRATIONS + ADMIN SEED ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    db.Database.Migrate();

    // If no admin exists in the DB, promote the first user OR create a seed admin.
    // Credentials come from config / environment so they're never hard-coded.
    var seedEmail = builder.Configuration["Seed:AdminEmail"];
    var seedPassword = builder.Configuration["Seed:AdminPassword"];

    if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword))
    {
        var anyAdmin = userManager.Users.Any(u => u.IsAdmin);
        if (!anyAdmin)
        {
            var existing = await userManager.FindByEmailAsync(seedEmail);
            if (existing == null)
            {
                var admin = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = seedEmail,
                    Email = seedEmail,
                    Name = "Admin",
                    IsAdmin = true,
                    RegisteredAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                };
                var result = await userManager.CreateAsync(admin, seedPassword);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to seed admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else if (!existing.IsAdmin)
            {
                existing.IsAdmin = true;
                await userManager.UpdateAsync(existing);
            }
        }
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<UserStatusMiddleware>();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
