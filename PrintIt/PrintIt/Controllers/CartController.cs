using Microsoft.AspNetCore.Mvc;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.Services;
using System.Security.Claims;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("/Cart")]
    public async Task<IActionResult> Cart()
    {
        ShopCart viewModel;

        if (User.Identity?.IsAuthenticated == true)
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                viewModel = new ShopCart(userId)
                {
                    Items = await _cartService.GetUserCartAsync(userId)
                };
                return View(viewModel);
            }
        }

        viewModel = new ShopCart(Guid.Empty); 
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(Guid id, List<PrintColor> selectedColors)
    {
        if (!TryGetUserId(out Guid userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _cartService.AddToCartAsync(userId, id, selectedColors);

        return Redirect(Request.Headers["Referer"].ToString() ?? "/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromCart(Guid id)
    {
        if (!TryGetUserId(out Guid userId))
            return RedirectToAction(nameof(Cart));

        await _cartService.RemoveFromCartAsync(userId, id);

        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
    {
        if (!TryGetUserId(out Guid userId))
            return Json(new { success = false });

        bool success = await _cartService.UpdateQuantityAsync(userId, id, quantity);

        return Json(new { success });
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdString, out userId);
    }
}