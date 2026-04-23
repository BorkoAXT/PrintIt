using PrintIt.Models;

namespace PrintIt.Services
{
    public interface IOrderService
    {
        Task<decimal> CalculateCartTotalAsync(Guid userId);
        Task<decimal> CalculateShippingAsync(decimal subtotal);
        Task<List<CartItem>> GetCartItemsAsync(Guid userId);
        Task<Order> CreateOrderAsync(Guid userId, string customerEmail, List<CartItem> items);
        Task ClearCartAsync(Guid userId);
        Task<List<Order>> GetOrdersByEmailAsync(string email);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<bool> OrderExistsForPaymentAsync(string paymentIntentId);
        Task MarkOrderPaidAsync(int orderId, string? paymentIntentId);
    }
}
