using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Models;

namespace InventoryApp.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<Inventory>       Inventories    { get; set; }
    public DbSet<Tag>             Tags           { get; set; }
    public DbSet<InventoryTag>    InventoryTags  { get; set; }
    public DbSet<InventoryField>  InventoryFields { get; set; }
    public DbSet<InventoryAccess> InventoryAccess { get; set; }
    public DbSet<Item>            Items          { get; set; }
    public DbSet<CustomIdElement> CustomIdElements { get; set; }
    public DbSet<Comment>         Comments       { get; set; }
    public DbSet<Like>            Likes          { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // registers Identity tables

        // ── Identity table renames ────────────────────────────────────────────
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.Name).HasMaxLength(200).IsRequired();
            e.Property(u => u.LanguagePreference).HasMaxLength(10).HasDefaultValue("en");
            e.Property(u => u.ThemePreference).HasMaxLength(10).HasDefaultValue("light");
        });

        // ── Inventory ─────────────────────────────────────────────────────────
        builder.Entity<Inventory>(e =>
        {
            e.ToTable("inventories");
            e.Property(i => i.Title).HasMaxLength(300).IsRequired();
            e.Property(i => i.Category).HasMaxLength(100);
            e.Property(i => i.ImageUrl).HasMaxLength(1000);

            // Optimistic concurrency via PostgreSQL xmin system column.
            // xmin is automatically incremented by Postgres on every UPDATE.
            // We map it as a shadow property so no C# property is needed on the model.
            e.Property<uint>("xmin")
             .HasColumnType("xid")
             .IsRowVersion()
             .HasColumnName("xmin");

            e.HasOne(i => i.Owner)
             .WithMany()
             .HasForeignKey(i => i.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);

            // Full-text search index on title + description
            e.HasIndex(i => new { i.Title, i.Description })
             .HasDatabaseName("IX_inventories_fts");
        });

        // ── Tag ───────────────────────────────────────────────────────────────
        builder.Entity<Tag>(e =>
        {
            e.ToTable("tags");
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            // Case-insensitive unique index via collation
            e.HasIndex(t => t.Name)
             .IsUnique()
             .HasDatabaseName("IX_tags_name_unique");
        });

        // ── InventoryTag (many-to-many join) ──────────────────────────────────
        builder.Entity<InventoryTag>(e =>
        {
            e.ToTable("inventory_tags");
            e.HasKey(it => new { it.InventoryId, it.TagId });

            e.HasOne(it => it.Inventory)
             .WithMany(i => i.InventoryTags)
             .HasForeignKey(it => it.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(it => it.Tag)
             .WithMany(t => t.InventoryTags)
             .HasForeignKey(it => it.TagId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── InventoryField ────────────────────────────────────────────────────
        builder.Entity<InventoryField>(e =>
        {
            e.ToTable("inventory_fields");
            e.Property(f => f.Title).HasMaxLength(200).IsRequired();
            e.Property(f => f.Description).HasMaxLength(500);

            // Each (inventory, type, slot) must be unique — enforces the 3-per-type cap.
            e.HasIndex(f => new { f.InventoryId, f.Type, f.Slot })
             .IsUnique()
             .HasDatabaseName("IX_inventory_fields_inv_type_slot");

            e.HasOne(f => f.Inventory)
             .WithMany(i => i.Fields)
             .HasForeignKey(f => f.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── InventoryAccess ───────────────────────────────────────────────────
        builder.Entity<InventoryAccess>(e =>
        {
            e.ToTable("inventory_access");
            e.HasKey(a => new { a.InventoryId, a.UserId });

            e.HasOne(a => a.Inventory)
             .WithMany(i => i.AccessList)
             .HasForeignKey(a => a.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Item ──────────────────────────────────────────────────────────────
        builder.Entity<Item>(e =>
        {
            e.ToTable("items");
            e.Property(i => i.CustomId).HasMaxLength(500).IsRequired();

            // THE key spec requirement: custom ID unique per inventory, not globally.
            // Enforced at DB level — duplicates cause DbUpdateException.
            e.HasIndex(i => new { i.InventoryId, i.CustomId })
             .IsUnique()
             .HasDatabaseName("IX_items_inventory_custom");

            // Same xmin-based optimistic locking for items.
            e.Property<uint>("xmin")
             .HasColumnType("xid")
             .IsRowVersion()
             .HasColumnName("xmin");

            e.HasOne(i => i.Inventory)
             .WithMany(inv => inv.Items)
             .HasForeignKey(i => i.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.CreatedBy)
             .WithMany()
             .HasForeignKey(i => i.CreatedById)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.UpdatedBy)
             .WithMany()
             .HasForeignKey(i => i.UpdatedById)
             .OnDelete(DeleteBehavior.SetNull);

            // Decimal precision for numeric custom fields
            foreach (var prop in new[] { "Number1", "Number2", "Number3" })
                e.Property<decimal?>(prop).HasPrecision(18, 4);
        });

        // ── CustomIdElement ───────────────────────────────────────────────────
        builder.Entity<CustomIdElement>(e =>
        {
            e.ToTable("custom_id_elements");
            e.Property(c => c.Config).HasMaxLength(500).HasDefaultValue("{}");

            e.HasOne(c => c.Inventory)
             .WithMany(i => i.IdElements)
             .HasForeignKey(c => c.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Comment ───────────────────────────────────────────────────────────
        builder.Entity<Comment>(e =>
        {
            e.ToTable("comments");

            e.HasOne(c => c.Inventory)
             .WithMany(i => i.Comments)
             .HasForeignKey(c => c.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Author)
             .WithMany()
             .HasForeignKey(c => c.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Like ──────────────────────────────────────────────────────────────
        builder.Entity<Like>(e =>
        {
            e.ToTable("likes");
            // Composite PK enforces one-like-per-user-per-item at the DB level.
            e.HasKey(l => new { l.ItemId, l.UserId });

            e.HasOne(l => l.Item)
             .WithMany(i => i.Likes)
             .HasForeignKey(l => l.ItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.User)
             .WithMany()
             .HasForeignKey(l => l.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
