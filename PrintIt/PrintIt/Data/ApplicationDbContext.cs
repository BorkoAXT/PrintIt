using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PrintIt.Enums;
using PrintIt.Models;
using System.Reflection.Emit;
using System.Text.Json;

namespace PrintIt.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Print> Prints { get; set; } = null!;
        public DbSet<ShopCart> ShopCarts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .HasOne(u => u.ShopCart)
                .WithOne(c => c.User)
                .HasForeignKey<ShopCart>(c => c.UserId);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.ShopCart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.ShopCartId);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Print)
                .WithMany()
                .HasForeignKey(ci => ci.PrintId);

            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.ShopCartId, ci.PrintId })
                .IsUnique();

            builder.Entity<Print>()
                .Property(p => p.Price)
                .HasPrecision(18, 4);

            builder.Entity<Print>()
            .Property(p => p.PrintColors)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<PrintColor>>(v, (JsonSerializerOptions)null) ?? new List<PrintColor>(),
                new ValueComparer<List<PrintColor>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );
        }

    }
}
