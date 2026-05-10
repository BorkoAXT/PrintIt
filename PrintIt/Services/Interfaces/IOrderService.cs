using PrintIt.Models;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        Task<decimal> CalculateCartTotalAsync(Guid userId);
        Task<decimal> CalculateShippingAsync(decimal subtotal);
        Task<List<CartItem>> GetCartItemsAsync(Guid userId);
        Task<Order> CreateOrderAsync(Guid userId, string customerEmail, List<CartItem> items);
        Task ClearCartAsync(Guid userId);
    }
}
