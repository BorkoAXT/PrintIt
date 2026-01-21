using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using PrintIt.Models;
using PrintIt.ViewModels;
using System.Diagnostics;
using System.Security.Claims;
using ErrorViewModel = PrintIt.ViewModels.ErrorViewModel;

namespace PrintIt.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            List<Print> wishlistPrints = new List<Print>();

            // Check if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                // Take user ID from claims
                string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    // Get wishlist items for the user
                    wishlistPrints = await _context.Set<UserWishlistItem>()
                        .Where(w => w.UserId == userId)
                        .Include(w => w.Print)
                        .Select(w => w.Print)
                        .ToListAsync();
                }
            }

            ViewData["WishlistItems"] = wishlistPrints;

            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
