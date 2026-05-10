using Data.Repositories;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<ShopCart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IMemoryCache _cache;

        public OrderService(
            IRepository<Order> orderRepository,
            IRepository<ShopCart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            ICartService cartService,
            IMemoryCache cache)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            _cache = cache;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(Guid userId)
        {
            return await _cartService.GetUserCartAsync(userId);
        }

        public async Task<decimal> CalculateCartTotalAsync(Guid userId)
        {
            var items = await GetCartItemsAsync(userId);
            return items.Sum(i => (i.Print?.Price ?? 0) * (i.Quantity > 0 ? i.Quantity : 1));
        }

        public Task<decimal> CalculateShippingAsync(decimal subtotal)
        {
            return Task.FromResult(subtotal > 50m || subtotal == 0 ? 0m : 5m);
        }

        public async Task<Order> CreateOrderAsync(Guid userId, string customerEmail, List<CartItem> items)
        {
            var order = new Order
            {
                CustomerEmail = customerEmail,
                Amount = items.Sum(i => (i.Print?.Price ?? 0) * i.Quantity),
                Currency = "EUR",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);
            await _cartItemRepository.DeleteBulkAsync(items);

            _cache.Remove($"cart-count-{userId}");

            return order;
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await _cartRepository.All()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null && cart.Items.Any())
            {
                await _cartItemRepository.DeleteBulkAsync(cart.Items.ToList());
                _cache.Remove($"cart-count-{userId}");
            }
        }
    }
}