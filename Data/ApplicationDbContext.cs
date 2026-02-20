using Microsoft.EntityFrameworkCore;
using InventoryManagementApp.Models;

namespace InventoryManagementApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email)
             .HasDatabaseName("ux_users_email")
             .IsUnique();

            e.Property(u => u.Email)
             .IsRequired();

            e.Property(u => u.RegisteredAt)
             .HasDefaultValueSql("NOW()");
        });
    }
}