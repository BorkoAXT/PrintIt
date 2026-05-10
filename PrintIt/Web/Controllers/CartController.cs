using Common.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly ICartService _cartService;

        public CartController(ILogger<CartController> logger, ICartService cartService)
        {
            _logger = logger;
            _cartService = cartService;
        }

        [HttpGet("/Cart")]
        public async Task<IActionResult> Cart()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    if (TryGetUserId(out Guid userId))
                    {
                        _logger.LogInformation("Fetching shopping cart for User ID: {UserId}", userId);

                        var items = await _cartService.GetUserCartAsync(userId);
                        var viewModel = new ShopCart(userId) { Items = items };

                        return View(viewModel);
                    }
                }

                _logger.LogWarning("Unauthenticated or invalid user attempted to access cart view.");
                return View(new ShopCart(Guid.Empty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shopping cart view.");
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid id, List<PrintColor> selectedColors)
        {
            if (!TryGetUserId(out Guid userId))
            {
                _logger.LogWarning("Anonymous user tried to add item {PrintId} to cart.", id);

                // Send JSON response to redirect the frontend to login
                var returnUrl = Url.Action(nameof(AddToCart), "Cart", new { id });
                return Json(new
                {
                    success = false,
                    redirectUrl = $"/Identity/Account/Login?returnUrl={returnUrl}"
                });
            }

            try
            {
                await _cartService.AddToCartAsync(userId, id, selectedColors);
                var totalItems = await _cartService.GetCartCountAsync(userId);

                _logger.LogInformation("User {UserId} added Print {PrintId} to cart. New total: {Count}", userId, id, totalItems);
                return Json(new { success = true, total = totalItems });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item {PrintId} to cart for User {UserId}", id, userId);
                return Json(new { success = false, message = "Could not add item to cart." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            if (!TryGetUserId(out Guid userId)) return RedirectToAction(nameof(Cart));

            try
            {
                await _cartService.RemoveFromCartAsync(userId, id);
                _logger.LogInformation("User {UserId} removed cart item {CartItemId}", userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item {CartItemId} for User {UserId}", id, userId);
            }

            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
        {
            if (!TryGetUserId(out Guid userId)) return Json(new { success = false });

            try
            {
                bool success = await _cartService.UpdateQuantityAsync(userId, id, quantity);

                if (success)
                    _logger.LogDebug("User {UserId} updated item {CartItemId} quantity to {Qty}", userId, id, quantity);
                else
                    _logger.LogWarning("User {UserId} failed to update quantity for item {CartItemId}", userId, id);

                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating quantity for item {CartItemId}", id);
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!TryGetUserId(out Guid userId)) return Json(0);

            var count = await _cartService.GetCartCountAsync(userId);
            return Json(count);
        }

        private bool TryGetUserId(out Guid userId)
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdString, out userId);
        }
    }
}