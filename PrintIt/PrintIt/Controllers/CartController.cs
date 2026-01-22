using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using PrintIt.Models;
using System.Security.Claims;

namespace PrintIt.Controllers
{
    public class CartController : BaseController
    {
        public CartController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/Cart")]
        public IActionResult Cart()
        {
            List<CartItem> userCart = new List<CartItem>();

            if (User.Identity?.IsAuthenticated == true)
            {
                string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    userCart = _context.ShopCarts
                        .Include(c => c.Items)
                        .ThenInclude(i => i.Print)
                        .Where(c => c.UserId == userId)
                        .SelectMany(c => c.Items).ToList();

                    ViewData["CartItems"] = userCart;
                }
            }

            ViewData["CartItems"] = userCart;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid id)
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var cart = await _context.ShopCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new ShopCart(userId);
                _context.ShopCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.PrintId == id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                _context.Entry(existingItem).State = EntityState.Modified;
            }
            else
            {
                var newItem = new CartItem(cart.Id, id);
                _context.CartItems.Add(newItem);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return RedirectToAction(nameof(Cart));
            }

            string referer = Request.Headers["Referer"].ToString();
            return string.IsNullOrEmpty(referer) ? RedirectToAction("Articles", "Store") : Redirect(referer);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userId)) return RedirectToAction(nameof(Cart));

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.PrintId == id && ci.ShopCart.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new { success = false });

            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userId)) return Json(new { success = false });

            var cartItem = await _context.CartItems 
                .FirstOrDefaultAsync(i => i.PrintId == id && i.ShopCart.UserId == userId);

            if (cartItem != null && quantity > 0)
            {
                cartItem.Quantity = quantity;
                _context.Update(cartItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
    }
}
