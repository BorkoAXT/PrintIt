using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PrintIt.Data;
using PrintIt.Models;

namespace PrintIt.Services
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
        public async Task AddToWishlistAsync(Guid userId, Guid printId)
        {
            var exists = await _context.UserWishlistItems
                .AnyAsync(w => w.UserId == userId && w.PrintId == printId);

            if (!exists)
            {
                _context.UserWishlistItems.Add(new UserWishlistItem(userId, printId));
                

                await _context.SaveChangesAsync();
            }
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

        public void ClearCache(Guid userId)
        {
            _cache.Remove($"Wishlist:{userId}");
        }
    }
}