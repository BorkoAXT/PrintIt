using Common.Enums;
using Data.Repositories;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Services
{
    public class CartService : ICartService
    {
        private readonly IRepository<ShopCart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IMemoryCache _cache;

        public CartService(
            IRepository<ShopCart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            IMemoryCache cache)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _cache = cache;
        }

        public async Task<List<CartItem>> GetUserCartAsync(Guid userId)
        {
            return await _cartItemRepository.All()
                .Where(i => i.ShopCart!.UserId == userId)
                .Include(i => i.Print)
                .ToListAsync();
        }

        public async Task AddToCartAsync(Guid userId, Guid printId, List<PrintColor> selectedColors)
        {
            var cart = await _cartRepository.All()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = await _cartRepository.AddAsync(new ShopCart(userId));
            }

            var incomingColors = selectedColors?.OrderBy(c => c).ToList() ?? new List<PrintColor>();
            var colorString = string.Join(',', incomingColors);

            var existingItem = cart.Items.FirstOrDefault(i =>
                i.PrintId == printId &&
                i.ColorString == colorString);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                await _cartItemRepository.UpdateAsync(existingItem);
            }
            else
            {
                var newItem = new CartItem(cart.Id, printId)
                {
                    Quantity = 1,
                    Colours = incomingColors
                };

                await _cartItemRepository.AddAsync(newItem);
            }

            InvalidateCache(userId);
        }

        public async Task<List<CartItem>> GetItemsByPrintIdAsync(Guid printId)
        {
            return await _cartItemRepository.All()
                .Where(ci => ci.PrintId == printId)
                .ToListAsync();
        }

        public async Task RemoveCartItemAsync(CartItem item)
        {
            await _cartItemRepository.DeleteAsync(item);
        }

        public async Task RemoveFromCartAsync(Guid userId, Guid cartItemId)
        {
            var cartItem = await _cartItemRepository.All()
                .FirstOrDefaultAsync(ci =>
                    ci.Id == cartItemId &&
                    ci.ShopCart!.UserId == userId);

            if (cartItem != null)
            {
                await _cartItemRepository.DeleteAsync(cartItem);
                InvalidateCache(userId);
            }
        }

        public async Task<bool> UpdateQuantityAsync(Guid userId, Guid cartItemId, int quantity)
        {
            var cartItem = await _cartItemRepository.All()
                .Include(i => i.ShopCart)
                .FirstOrDefaultAsync(i =>
                    i.Id == cartItemId &&
                    i.ShopCart!.UserId == userId);

            if (cartItem == null || quantity <= 0)
            {
                return false;
            }

            cartItem.Quantity = quantity;
            await _cartItemRepository.UpdateAsync(cartItem);

            InvalidateCache(userId);
            return true;
        }

        public async Task<int> GetCartCountAsync(Guid userId)
        {
            string key = $"cart-count-{userId}";

            int? result = await _cache.GetOrCreateAsync<int?>(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

                return await _cartItemRepository.All()
                    .Where(i => i.ShopCart!.UserId == userId)
                    .SumAsync(i => i.Quantity);
            });

            return result ?? 0;
        }

        public async Task RemoveAllByPrintIdAsync(Guid printId)
        {
            var affectedUserIds = await _cartItemRepository.All()
                .Where(ci => ci.PrintId == printId)
                .Select(ci => ci.ShopCart!.UserId)
                .Distinct()
                .ToListAsync();

            var items = await _cartItemRepository.All()
                .Where(ci => ci.PrintId == printId)
                .ToListAsync();

            if (items.Any())
            {
                await _cartItemRepository.DeleteBulkAsync(items);

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
}