using Entities.Models;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Diagnostics;
using System.Security.Claims;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWishlistService _wishlistService;

        public HomeController(ILogger<HomeController> logger, IWishlistService wishlistService)
        {
            _logger = logger;
            _wishlistService = wishlistService;
        }

        public async Task<IActionResult> Index()
        {
            List<UserWishlistItem> wishlistPrints = new();

            try
            {
                if (TryGetUserId(out Guid userId))
                {
                    _logger.LogInformation("Loading landing page for User: {UserId}", userId);
                    wishlistPrints = await _wishlistService.GetWishlistForUserAsync(userId);
                }
                else
                {
                    _logger.LogDebug("Loading landing page for anonymous guest.");
                }

                return View(wishlistPrints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the Index page.");
                return RedirectToAction(nameof(Error));
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            _logger.LogWarning("Error page triggered. Request ID: {RequestId}", requestId);

            return View(new ErrorViewModel
            {
                RequestId = requestId
            });
        }

        private bool TryGetUserId(out Guid userId)
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdString, out userId);
        }
    }
}