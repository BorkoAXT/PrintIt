using PrintIt.Enums;
using PrintIt.Models;

namespace PrintIt.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetUserCartAsync(Guid userId);
        Task<List<CartItem>> GetItemsByPrintIdAsync(Guid printId);
        Task AddToCartAsync(Guid userId, Guid printId, List<PrintColor> colors);
        Task RemoveFromCartAsync(Guid userId, Guid printId);
        Task RemoveCartItemAsync(CartItem item);
        Task<bool> UpdateQuantityAsync(Guid userId, Guid printId, int quantity);
        Task<int> GetCartCountAsync(Guid userId);
    }
}
