using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PrintIt.Data;
using PrintIt.Models;
using PrintIt.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly IMemoryCache _cache;
    public OrderService(ApplicationDbContext context, ICartService cartService, IMemoryCache cache)
    {
        _context = context;
        _cartService = cartService;
        _cache = cache;
    }

    public async Task<List<CartItem>> GetCartItemsAsync(Guid userId)
    {
        return await _cartService.GetUserCartAsync(userId);
    }

    public async Task<decimal> CalculateCartTotalAsync(Guid userId)
    {
        var items = await GetCartItemsAsync(userId);
        return items.Sum(i => (i.Print?.Price ?? 0) * (i.Quantity > 0 ? i.Quantity : 1));
    }

    public Task<decimal> CalculateShippingAsync(decimal subtotal)
    {
        return Task.FromResult((subtotal > 50m || subtotal == 0) ? 0m : 5m);
    }

    public async Task<Order> CreateOrderAsync(Guid userId, string customerEmail, List<CartItem> items)
    {
        var order = new Order
        {
            CustomerEmail = customerEmail,
            Amount = items.Sum(i => (i.Print?.Price ?? 0) * i.Quantity),
            Currency = "EUR",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cart = await _context.ShopCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null && cart.Items.Any())
        {
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            string key = $"cart-count-{userId}";
            _cache.Remove(key);
        }
    }
}