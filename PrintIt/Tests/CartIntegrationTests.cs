using Common.Enums;
using Services.Interfaces;

namespace Tests;

public class CartIntegrationTests : IClassFixture<PrintItFactory>
{
    private readonly PrintItFactory _factory;

    public CartIntegrationTests(PrintItFactory factory)
    {
        _factory = factory;
    }

    // Helper to create a test Print
    private Print CreateTestPrint(string name = "Test Print")
    {
        return new Print(
            name,
            "Test Description",
            MaterialType.PLA,
            100,          // weight
            10,           // price
            PrintType.Accessory,
            new List<PrintColor> { PrintColor.Red, PrintColor.Blue }
        );
    }

    // Helper to create a dummy user for FK
    private async Task<Guid> CreateTestUser(ApplicationDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com"
        });
        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task AddToCartAsync_ShouldCreateNewCartItemRows()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        // 1️⃣ Create dummy user
        var userId = await CreateTestUser(db);

        // 2️⃣ Create ShopCart
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        // 3️⃣ Create Print
        var print = CreateTestPrint();
        db.Prints.Add(print);

        await db.SaveChangesAsync(); // save parents first

        // 4️⃣ Add to cart via service
        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Red });

        // 5️⃣ Assert
        var itemsInDb = await db.CartItems
            .Where(i => i.ShopCartId == cart.Id && i.PrintId == print.Id)
            .ToListAsync();

        Assert.NotEmpty(itemsInDb);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldDeleteAllRowsAndInvalidateCache()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        // 1️⃣ Create dummy user
        var userId = await CreateTestUser(db);

        var cacheKey = $"cart-count-{userId}";

        // 2️⃣ Create ShopCart
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        // 3️⃣ Create Prints
        var print1 = CreateTestPrint("Print1");
        var print2 = CreateTestPrint("Print2");
        db.Prints.AddRange(print1, print2);

        await db.SaveChangesAsync();

        // 4️⃣ Create CartItems
        db.CartItems.Add(new CartItem(cart.Id, print1.Id));
        db.CartItems.Add(new CartItem(cart.Id, print2.Id));

        await db.SaveChangesAsync();

        // 5️⃣ Seed cache
        cache.Set(cacheKey, 2);

        // Act
        await orderService.ClearCartAsync(userId);

        // Assert
        var remainingCount = await db.CartItems.CountAsync(i => i.ShopCartId == cart.Id);
        var cacheExists = cache.TryGetValue(cacheKey, out _);

        Assert.Equal(0, remainingCount);
        Assert.False(cacheExists, "The cache should be removed when the cart is cleared.");
    }

    [Fact]
    public async Task GetCartCountAsync_ShouldReturnTotalRowsInUserCart()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        // 1️⃣ Create dummy user
        var userId = await CreateTestUser(db);

        // 2️⃣ Create ShopCart
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        // 3️⃣ Create Prints
        var print1 = CreateTestPrint("Print1");
        var print2 = CreateTestPrint("Print2");
        var print3 = CreateTestPrint("Print3");
        db.Prints.AddRange(print1, print2, print3);

        await db.SaveChangesAsync();

        // 4️⃣ Create CartItems
        db.CartItems.Add(new CartItem(cart.Id, print1.Id) { Quantity = 1 });
        db.CartItems.Add(new CartItem(cart.Id, print2.Id) { Quantity = 1 });
        db.CartItems.Add(new CartItem(cart.Id, print3.Id) { Quantity = 1 });

        await db.SaveChangesAsync();

        // Act
        var count = await cartService.GetCartCountAsync(userId);

        // Assert
        Assert.Equal(3, count);
    }
    [Fact]
    public async Task AddToCartAsync_SamePrint_ShouldIncrementQuantity()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var userId = await CreateTestUser(db);

        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        var print = CreateTestPrint("MultiPrint");
        db.Prints.Add(print);

        await db.SaveChangesAsync();

        // Add same print twice
        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Red });
        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Red });

        var cartItem = await db.CartItems.FirstOrDefaultAsync(i => i.ShopCartId == cart.Id && i.PrintId == print.Id);

        Assert.NotNull(cartItem);
        Assert.Equal(2, cartItem!.Quantity);
    }
    [Fact]
    public async Task AddToCartAsync_MultipleColors_ShouldStoreAllColors()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var userId = await CreateTestUser(db);

        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        var print = CreateTestPrint("ColorPrint");
        db.Prints.Add(print);

        await db.SaveChangesAsync();

        var colors = new List<PrintColor> { PrintColor.Red, PrintColor.Blue, PrintColor.Green };
        await cartService.AddToCartAsync(userId, print.Id, colors);

        var cartItem = await db.CartItems.FirstOrDefaultAsync(i => i.ShopCartId == cart.Id && i.PrintId == print.Id);

        Assert.NotNull(cartItem);
        Assert.Equal(3, cartItem!.Colours.Count);
        Assert.Contains(PrintColor.Green, cartItem.Colours);
    }
    [Fact]
    public async Task ClearCartAsync_EmptyCart_ShouldNotFail()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var userId = await CreateTestUser(db);

        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);
        await db.SaveChangesAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => orderService.ClearCartAsync(userId));

        Assert.Null(exception);
    }
    [Fact]
    public async Task GetCartCountAsync_EmptyCart_ShouldReturnZero()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var userId = await CreateTestUser(db);

        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);
        await db.SaveChangesAsync();

        var count = await cartService.GetCartCountAsync(userId);

        Assert.Equal(0, count);
    }
}