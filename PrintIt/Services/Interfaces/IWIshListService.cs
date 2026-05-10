using Entities.Models;

namespace Services.Interfaces
{
    public interface IWishlistService
    {
        Task<List<UserWishlistItem>> GetWishlistForUserAsync(Guid userId);
        Task<List<UserWishlistItem>> GetItemsByPrintIdAsync(Guid printId);
        Task<bool> IsFavoriteAsync(Guid userId, Guid printId);
        Task<bool> ToggleAsync(Guid userId, Guid printId);
        Task RemoveAsync(UserWishlistItem item);
        Task RemoveAllByPrintIdAsync(Guid printId);
        void ClearCache(Guid userId);
    }
}
