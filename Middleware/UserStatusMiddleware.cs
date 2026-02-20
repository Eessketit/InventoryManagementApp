using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using InventoryManagementApp.Data;
using InventoryManagementApp.Models;

namespace InventoryManagementApp.Middleware
{
    public class UserStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Allow anonymous access to auth pages and static files
            if (path != null &&
                (path.StartsWith("/auth/login")
                 || path.StartsWith("/auth/register")
                 || path.StartsWith("/auth/logout")
                 || path.StartsWith("/css")
                 || path.StartsWith("/js")
                 || path.StartsWith("/lib")))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    await ForceLogout(context);
                    return;
                }

                var user = await db.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Id == userId);

                if (user == null || user.Status == UserStatus.Blocked)
                {
                    await ForceLogout(context);
                    return;
                }
            }

            await _next(context);
        }

        private static async Task ForceLogout(HttpContext context)
        {
            await context.SignOutAsync();

            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location = "/Auth/Login";
        }
    }
}