using Microsoft.EntityFrameworkCore;
using InventoryManagementApp.Data;
using InventoryManagementApp.Middleware;
using InventoryManagementApp.Services;

var builder = WebApplication.CreateBuilder(args);

// KESTREL (Render)
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// DATABASE
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
        $"SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new Exception("No database connection string configured.");
}

Console.WriteLine(!string.IsNullOrWhiteSpace(databaseUrl)
    ? $"[Startup] Using DATABASE_URL (Host: {new Uri(databaseUrl).Host})"
    : "[Startup] DATABASE_URL not found — using appsettings connection string");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// AUTH
builder.Services
    .AddAuthentication(
        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// AUTO MIGRATIONS
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<UserStatusMiddleware>();
app.UseAuthorization();

app.MapRazorPages();
app.Run();