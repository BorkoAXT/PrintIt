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

            // Check if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                // Take user ID from claims
                string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    // Get wishlist items for the user
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
    }
}
