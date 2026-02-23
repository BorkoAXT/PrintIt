using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PrintIt.Enums;
using PrintIt.Models;
using System.Text.Json;

namespace PrintIt.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Print> Prints { get; set; } = null!;
        public DbSet<ShopCart> ShopCarts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<UserWishlistItem> UserWishlistItems { get; set; } = null!;

        public DbSet<Order> Orders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User <-> ShopCart (1–1)
            builder.Entity<User>()
                .HasOne(u => u.ShopCart)
                .WithOne(c => c.User)
                .HasForeignKey<ShopCart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShopCart <-> CartItem (1–N)
            builder.Entity<CartItem>()
                .HasOne(ci => ci.ShopCart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.ShopCartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem <-> Print (N–1)
            builder.Entity<CartItem>()
                .HasOne(ci => ci.Print)
                .WithMany()
                .HasForeignKey(ci => ci.PrintId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.ShopCartId, ci.PrintId })
                .IsUnique();

            // UserWishlistItem (N–M)
            builder.Entity<UserWishlistItem>()
                .HasKey(w => new { w.UserId, w.PrintId });

            builder.Entity<UserWishlistItem>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlist)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserWishlistItem>()
                .HasOne(w => w.Print)
                .WithMany()
                .HasForeignKey(w => w.PrintId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>(entity =>
            {
                // Update the Index to include Colours to prevent the SqlException
                entity.HasIndex(ci => new { ci.ShopCartId, ci.PrintId, ci.Colours })
                    .IsUnique();

                // Add the ValueComparer to fix the Validation[10620] error
                entity.Property(e => e.Colours)
                    .HasConversion(
                        v => string.Join(',', v.Select(c => (int)c)),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => (PrintColor)int.Parse(s))
                            .ToList(),
                        new ValueComparer<List<PrintColor>>(
                            (c1, c2) => c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()
                        )
                    );
            });

            // Print configuration
            builder.Entity<Print>()
                .Property(p => p.Price)
                .HasPrecision(18, 4);


            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.ShopCartId, ci.PrintId })
                .IsUnique();

            builder.Entity<Print>()
                .Property(p => p.PrintColors)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<PrintColor>>(v, (JsonSerializerOptions)null)
                        ?? new List<PrintColor>(),
                    new ValueComparer<List<PrintColor>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                    )
                );
        }
    }
}
