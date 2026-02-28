using PrintIt.Models;

namespace PrintIt.Services
{
    public interface IWishlistService
    {
        Task<List<UserWishlistItem>> GetWishlistForUserAsync(Guid userId);
        Task<List<UserWishlistItem>> GetItemsByPrintIdAsync(Guid printId);
        Task<bool> IsFavoriteAsync(Guid userId, Guid printId);
        Task<bool> ToggleAsync(Guid userId, Guid printId);
        Task RemoveAsync(UserWishlistItem item);
    }
}
