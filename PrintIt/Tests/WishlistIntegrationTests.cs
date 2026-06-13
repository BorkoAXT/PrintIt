using Common.Enums;
using Services.Interfaces;

namespace Tests;

public class WishlistServiceTests : IClassFixture<PrintItFactory>
{
    private readonly PrintItFactory _factory;

    public WishlistServiceTests(PrintItFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> CreateTestUser(ApplicationDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = $"user{userId}@example.com" });
        await db.SaveChangesAsync();
        return userId;
    }

    private Print CreateTestPrint(string name = "Wishlist Print")
    {
        return new Print(
            name,
            "Description",
            MaterialType.PLA,
            100,
            5,

            PrintType.Accessory,
            new List<PrintColor> { PrintColor.Red, PrintColor.Blue }
        );
    }

    // -------------------------------
    [Fact]
    public async Task ToggleAsync_ShouldAddAndRemoveItem_AndInvalidateCache()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wishlist = scope.ServiceProvider.GetRequiredService<IWishlistService>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var userId = Guid.NewGuid();
        var print = new Print(
            "TestPrint", "Desc", MaterialType.PLA, 100, 5,
            PrintType.Accessory, new List<PrintColor> { PrintColor.Red });

        // ✅ Add the parent entities first
        db.Users.Add(new User { Id = userId, Email = $"user{userId}@example.com" });
        db.Prints.Add(print);
        await db.SaveChangesAsync(); // must save parents first

        var cacheKey = $"Wishlist:{userId}";
        cache.Set(cacheKey, 123);

        // Add item
        var added = await wishlist.ToggleAsync(userId, print.Id);
        Assert.True(added);
        Assert.False(cache.TryGetValue(cacheKey, out _)); // cache cleared

        // Remove item
        cache.Set(cacheKey, 456);
        var removed = await wishlist.ToggleAsync(userId, print.Id);
        Assert.False(removed);
        Assert.False(cache.TryGetValue(cacheKey, out _));
    }

    // -------------------------------
    [Fact]
    public async Task ToggleAsync_ShouldNotDuplicateItem()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wishlist = scope.ServiceProvider.GetRequiredService<IWishlistService>();

        var userId = await CreateTestUser(db);
        var print = CreateTestPrint();
        db.Prints.Add(print);
        await db.SaveChangesAsync();

        // First toggle → add
        await wishlist.ToggleAsync(userId, print.Id);

        // Second toggle → remove
        await wishlist.ToggleAsync(userId, print.Id);

        // Third toggle → add again
        await wishlist.ToggleAsync(userId, print.Id);

        var items = await wishlist.GetWishlistForUserAsync(userId);
        Assert.Single(items); // only one row exists
        Assert.Equal(print.Id, items[0].PrintId);
    }

    // -------------------------------
    [Fact]
    public async Task GetItemsByPrintIdAsync_ShouldReturnAllUsersWithPrint()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wishlist = scope.ServiceProvider.GetRequiredService<IWishlistService>();

        var user1 = await CreateTestUser(db);
        var user2 = await CreateTestUser(db);
        var print = CreateTestPrint();
        db.Prints.Add(print);
        await db.SaveChangesAsync();

        await wishlist.ToggleAsync(user1, print.Id); // add
        await wishlist.ToggleAsync(user2, print.Id); // add

        var items = await wishlist.GetItemsByPrintIdAsync(print.Id);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.UserId == user1);
        Assert.Contains(items, i => i.UserId == user2);
    }

    // -------------------------------
    [Fact]
    public async Task ClearCache_ShouldRemoveUserCache()
    {
        using var scope = _factory.Services.CreateScope();
        var wishlist = scope.ServiceProvider.GetRequiredService<IWishlistService>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var userId = Guid.NewGuid();
        var cacheKey = $"Wishlist:{userId}";
        cache.Set(cacheKey, 123);

        wishlist.ClearCache(userId);
        Assert.False(cache.TryGetValue(cacheKey, out _));
    }
}