using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PrintIt.Data;
using PrintIt.Enums;
using PrintIt.Models;
using Services.Interfaces;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public CartService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<CartItem>> GetUserCartAsync(Guid userId)
    {
        return await _context.ShopCarts
            .Include(c => c.Items)
            .ThenInclude(i => i.Print)
            .Where(c => c.UserId == userId)
            .SelectMany(c => c.Items)
            .ToListAsync();
    }

    public async Task AddToCartAsync(Guid userId, Guid printId, List<PrintColor> selectedColors)
    {
        var cart = await _context.ShopCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new ShopCart(userId);
            _context.ShopCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var incomingColors = selectedColors?.OrderBy(c => c).ToList() ?? new List<PrintColor>();
        var colorString = string.Join(',', incomingColors);

        var existingItem = cart.Items.FirstOrDefault(i =>
            i.PrintId == printId &&
            i.ColorString == colorString
        );

        if (existingItem != null)
        {
            existingItem.Quantity++;
            _context.Entry(existingItem).State = EntityState.Modified;
        }
        else
        {
            var newItem = new CartItem(cart.Id, printId)
            {
                Quantity = 1,
                Colours = incomingColors
            };
            _context.CartItems.Add(newItem);
        }

        await _context.SaveChangesAsync();

        InvalidateCache(userId);
    }

    public async Task<List<CartItem>> GetItemsByPrintIdAsync(Guid printId)
    {
        return await _context.CartItems
            .Where(ci => ci.PrintId == printId)
            .ToListAsync();
    }

    public async Task RemoveCartItemAsync(CartItem item)
    {
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveFromCartAsync(Guid userId, Guid cartItemId)
    {
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci =>
                ci.Id == cartItemId &&
                ci.ShopCart.UserId == userId);

        if (cartItem != null)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            InvalidateCache(userId);
        }
    }

    public async Task<bool> UpdateQuantityAsync(Guid userId, Guid cartItemId, int quantity)
    {
        var cartItem = await _context.CartItems
            .Include(i => i.ShopCart)
            .FirstOrDefaultAsync(i =>
                i.Id == cartItemId &&
                i.ShopCart.UserId == userId);

        if (cartItem == null || quantity <= 0)
            return false;

        cartItem.Quantity = quantity;
        await _context.SaveChangesAsync();

        InvalidateCache(userId);
        return true;
    }

    public async Task<int> GetCartCountAsync(Guid userId)
    {
        string key = $"cart-count-{userId}";

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

            return await _context.CartItems
                .Where(i => i.ShopCart.UserId == userId)
                .SumAsync(i => i.Quantity);
        });
    }
    public async Task RemoveAllByPrintIdAsync(Guid printId)
    {
        var affectedUserIds = await _context.CartItems
            .Where(ci => ci.PrintId == printId)
            .Select(ci => ci.ShopCart.UserId)
            .Distinct()
            .ToListAsync();

        var items = await _context.CartItems.Where(ci => ci.PrintId == printId).ToListAsync();
        if (items.Any())
        {
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            foreach (var userId in affectedUserIds)
            {
                InvalidateCache(userId);
                _cache.Remove($"cart-items-{userId}");
            }
        }
    }

    private void InvalidateCache(Guid userId)
    {
        _cache.Remove($"cart-count-{userId}");
    }
    
}