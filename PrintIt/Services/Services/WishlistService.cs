using Data.Repositories;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<UserWishlistItem> _wishlistRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _userCacheDuration = TimeSpan.FromSeconds(60);

        public WishlistService(
            IRepository<UserWishlistItem> wishlistRepository,
            IRepository<CartItem> cartItemRepository,
            IMemoryCache cache)
        {
            _wishlistRepository = wishlistRepository;
            _cartItemRepository = cartItemRepository;
            _cache = cache;
        }

        public async Task<bool> IsFavoriteAsync(Guid userId, Guid printId)
        {
            var wishlist = await GetWishlistForUserAsync(userId);
            return wishlist.Any(w => w.PrintId == printId);
        }

        public async Task<bool> ToggleAsync(Guid userId, Guid printId)
        {
            var wishlistItem = await _wishlistRepository.All()
                .FirstOrDefaultAsync(w => w.UserId == userId && w.PrintId == printId);

            if (wishlistItem != null)
            {
                await _wishlistRepository.DeleteAsync(wishlistItem);
                ClearCache(userId);
                return false;
            }

            await _wishlistRepository.AddAsync(new UserWishlistItem(userId, printId));
            ClearCache(userId);
            return true;
        }

        public async Task<List<UserWishlistItem>> GetItemsByPrintIdAsync(Guid printId)
        {
            return await _wishlistRepository.All()
                .Where(w => w.PrintId == printId)
                .ToListAsync();
        }

        public async Task RemoveAsync(UserWishlistItem item)
        {
            await _wishlistRepository.DeleteAsync(item);
        }

        public async Task<List<UserWishlistItem>> GetWishlistForUserAsync(Guid userId)
        {
            return await _cache.GetOrCreateAsync($"Wishlist:{userId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _userCacheDuration;

                return await _wishlistRepository.All()
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Print)
                    .ToListAsync();
            }) ?? new List<UserWishlistItem>();
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
                    ClearCache(userId);
                }
            }
        }

        public void ClearCache(Guid userId)
        {
            _cache.Remove($"Wishlist:{userId}");
        }
    }
}