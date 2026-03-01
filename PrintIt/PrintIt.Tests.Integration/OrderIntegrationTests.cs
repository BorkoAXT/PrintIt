using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PrintIt.Enums;
using PrintIt.Models;

namespace PrintIt.Tests.Integration;

public class OrderIntegrationTests : IClassFixture<PrintItFactory>
{
    private readonly PrintItFactory _factory;

    public OrderIntegrationTests(PrintItFactory factory)
    {
        _factory = factory;
    }

    // Helpers
    private Print CreateTestPrint(string name = "Test Print")
    {
        return new Print(
            name,
            "Test Description",
            MaterialType.PLA,
            100,
            10,
            "test.stl",
            PrintType.Accessory,
            new List<PrintColor> { PrintColor.Red, PrintColor.Blue }
        );
    }

    private async Task<Guid> CreateTestUser(ApplicationDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "test@example.com" });
        await db.SaveChangesAsync();
        return userId;
    }

    // -----------------------------
    // 1️⃣ Create Order from Cart
    // -----------------------------
    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderWithCorrectAmount()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var userId = await CreateTestUser(db);

        // Setup cart
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        var print1 = CreateTestPrint("Print1");
        var print2 = CreateTestPrint("Print2");
        db.Prints.AddRange(print1, print2);

        await db.SaveChangesAsync();

        // Add items to cart
        await cartService.AddToCartAsync(userId, print1.Id, new List<PrintColor> { PrintColor.Red });
        await cartService.AddToCartAsync(userId, print2.Id, new List<PrintColor> { PrintColor.Blue });

        var items = await db.CartItems.Where(i => i.ShopCartId == cart.Id).ToListAsync();

        // Act
        var order = await orderService.CreateOrderAsync(userId, "test@example.com", items);

        // Assert
        Assert.NotNull(order);
        // Amount should equal sum of item prices
        var expectedAmount = items.Sum(i => i.Print!.Price * i.Quantity);
        Assert.Equal(expectedAmount, order.Amount);

        // Cart should be cleared
        var remainingCartItems = await db.CartItems.CountAsync(i => i.ShopCartId == cart.Id);
        Assert.Equal(0, remainingCartItems);
    }

    // -----------------------------
    // 2️⃣ Multiple Users Order Isolation
    // -----------------------------
    [Fact]
    public async Task MultipleUsers_ShouldHaveIndependentOrders()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var user1 = await CreateTestUser(db);
        var user2 = await CreateTestUser(db);

        var cart1 = new ShopCart(user1);
        var cart2 = new ShopCart(user2);
        db.ShopCarts.AddRange(cart1, cart2);

        var print1 = CreateTestPrint("Print1");
        var print2 = CreateTestPrint("Print2");
        db.Prints.AddRange(print1, print2);

        await db.SaveChangesAsync();

        await cartService.AddToCartAsync(user1, print1.Id, new List<PrintColor> { PrintColor.Red });
        await cartService.AddToCartAsync(user2, print2.Id, new List<PrintColor> { PrintColor.Blue });

        var items1 = await db.CartItems.Where(i => i.ShopCartId == cart1.Id).ToListAsync();
        var items2 = await db.CartItems.Where(i => i.ShopCartId == cart2.Id).ToListAsync();

        var order1 = await orderService.CreateOrderAsync(user1, "user1@example.com", items1);
        var order2 = await orderService.CreateOrderAsync(user2, "user2@example.com", items2);

        Assert.NotEqual(order1.Id, order2.Id);
        Assert.Equal(items1.Sum(i => i.Print!.Price * i.Quantity), order1.Amount);
        Assert.Equal(items2.Sum(i => i.Print!.Price * i.Quantity), order2.Amount);
    }

    // -----------------------------
    // 3️⃣ Clear Cart Should Not Throw
    // -----------------------------
    [Fact]
    public async Task ClearCartAsync_WhenCartIsEmpty_ShouldNotThrow()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var userId = await CreateTestUser(db);
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);
        await db.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(() => orderService.ClearCartAsync(userId));

        Assert.Null(exception);
    }
    [Fact]
    public async Task CreateOrderAsync_EmptyCart_ShouldCreateOrderWithZeroAmount()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var userId = Guid.NewGuid();
        var order = await orderService.CreateOrderAsync(userId, "empty@example.com", new List<CartItem>());

        Assert.NotNull(order);
        Assert.Equal(0, order.Amount);
        Assert.Equal("Pending", order.Status);
    }
    [Fact]
    public async Task CreateOrderAsync_MultipleQuantities_ShouldCalculateAmountCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var userId = await CreateTestUser(db);
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        var print = CreateTestPrint("Print1");
        db.Prints.Add(print);
        await db.SaveChangesAsync();

        // Add twice with same color → quantity increments to 2
        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Red });
        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Red });

        // Add once with different color → new row
        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Blue });

        // Load tracked items including Print
        var items = await db.CartItems.Include(i => i.Print).Where(i => i.ShopCartId == cart.Id).ToListAsync();
        var expectedAmount = items.Sum(i => i.Print!.Price * i.Quantity);

        var order = await orderService.CreateOrderAsync(userId, "multiqty@example.com", items);

        Assert.Equal(expectedAmount, order.Amount);
        Assert.Equal(0, await db.CartItems.CountAsync(i => i.ShopCartId == cart.Id));
    }
    [Fact]
    public async Task CreateOrderAsync_ShouldInvalidateCartCache()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var userId = await CreateTestUser(db);
        var cart = new ShopCart(userId);
        db.ShopCarts.Add(cart);

        var print = CreateTestPrint("CachedPrint");
        db.Prints.Add(print);
        await db.SaveChangesAsync();

        await cartService.AddToCartAsync(userId, print.Id, new List<PrintColor> { PrintColor.Red });

        // Set cache
        var cacheKey = $"cart-count-{userId}";
        cache.Set(cacheKey, 99);

        // Load tracked items
        var items = await db.CartItems.Include(i => i.Print).Where(i => i.ShopCartId == cart.Id).ToListAsync();
        await orderService.CreateOrderAsync(userId, "cache@example.com", items);

        // Cart cleared
        Assert.Equal(0, await db.CartItems.CountAsync(i => i.ShopCartId == cart.Id));
        // Cache cleared
        Assert.False(cache.TryGetValue(cacheKey, out _));
    }
    [Fact]
    public async Task MultipleUsers_ShouldCreateIndependentOrders()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var user1 = await CreateTestUser(db);
        var user2 = await CreateTestUser(db);

        var cart1 = new ShopCart(user1);
        var cart2 = new ShopCart(user2);
        db.ShopCarts.AddRange(cart1, cart2);

        var print1 = CreateTestPrint("Print1");
        var print2 = CreateTestPrint("Print2");
        db.Prints.AddRange(print1, print2);
        await db.SaveChangesAsync();

        // Add items
        await cartService.AddToCartAsync(user1, print1.Id, new List<PrintColor> { PrintColor.Red });
        await cartService.AddToCartAsync(user2, print2.Id, new List<PrintColor> { PrintColor.Blue });

        // Load tracked items
        var items1 = await db.CartItems.Include(i => i.Print).Where(i => i.ShopCartId == cart1.Id).ToListAsync();
        var items2 = await db.CartItems.Include(i => i.Print).Where(i => i.ShopCartId == cart2.Id).ToListAsync();

        var expectedAmount1 = items1.Sum(i => i.Print!.Price * i.Quantity);
        var expectedAmount2 = items2.Sum(i => i.Print!.Price * i.Quantity);

        var order1 = await orderService.CreateOrderAsync(user1, "user1@example.com", items1);
        var order2 = await orderService.CreateOrderAsync(user2, "user2@example.com", items2);

        Assert.Equal(expectedAmount1, order1.Amount);
        Assert.Equal(expectedAmount2, order2.Amount);
        Assert.NotEqual(order1.Id, order2.Id);

        // Both carts cleared
        Assert.Equal(0, await db.CartItems.CountAsync(i => i.ShopCartId == cart1.Id));
        Assert.Equal(0, await db.CartItems.CountAsync(i => i.ShopCartId == cart2.Id));
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldSetDefaultStatusAndCurrency()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var userId = Guid.NewGuid();
        var order = await orderService.CreateOrderAsync(userId, "default@example.com", new List<CartItem>());

        Assert.Equal("Pending", order.Status);
        Assert.Equal("EUR", order.Currency);
        Assert.True((DateTime.UtcNow - order.CreatedAt).TotalSeconds < 5); // recently created
    }
}