using Microsoft.AspNetCore.Mvc;
using PrintIt.Models;
using PrintIt.Services;
using PrintIt.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

public class HomeController : Controller
{
    private readonly IWishlistService _wishlistService;

    public HomeController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    public async Task<IActionResult> Index()
    {
        List<UserWishlistItem> wishlistPrints = new();

        if (TryGetUserId(out Guid userId))
        {
            wishlistPrints = await _wishlistService.GetWishlistForUserAsync(userId);
        }

        return View(wishlistPrints);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdString, out userId);
    }
}