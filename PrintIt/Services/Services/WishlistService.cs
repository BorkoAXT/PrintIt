using Data;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _userCacheDuration = TimeSpan.FromSeconds(60);

        public WishlistService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> IsFavoriteAsync(Guid userId, Guid printId)
        {
            var wishlist = await GetWishlistForUserAsync(userId);
            return wishlist.Any(w => w.PrintId == printId);
        }

        public async Task<bool> ToggleAsync(Guid userId, Guid printId)
        {
            var wishlistItem = await _context.UserWishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.PrintId == printId);

            if (wishlistItem != null)
            {
                _context.UserWishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                ClearCache(userId);
                return false;
            }
            else
            {
                _context.UserWishlistItems.Add(new UserWishlistItem(userId, printId));
                await _context.SaveChangesAsync();
                ClearCache(userId);
                return true;
            }
        }

        public async Task<List<UserWishlistItem>> GetItemsByPrintIdAsync(Guid printId)
        {
            return await _context.UserWishlistItems
                .Where(w => w.PrintId == printId)
                .ToListAsync();
        }

        public async Task RemoveAsync(UserWishlistItem item)
        {
            _context.UserWishlistItems.Remove(item);
            await _context.SaveChangesAsync();
        }
        public async Task<List<UserWishlistItem>> GetWishlistForUserAsync(Guid userId)
        {
            return await _cache.GetOrCreateAsync($"Wishlist:{userId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _userCacheDuration;

                return await _context.UserWishlistItems
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Print)   // ✅ THIS FIXES EVERYTHING
                    .ToListAsync();
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