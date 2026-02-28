using Microsoft.AspNetCore.Mvc;
using PrintIt.Services;
using System.Security.Claims;

public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public OrderController(IOrderService orderService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewData["UserId"] = userIdString;

        return View(); // This will render Views/Order/Index.cshtml
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        if (!TryGetUserId(out var userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var items = await _orderService.GetCartItemsAsync(userId);
        if (!items.Any())
            return RedirectToAction("Cart", "Cart");

        decimal subtotal = await _orderService.CalculateCartTotalAsync(userId);
        decimal shipping = await _orderService.CalculateShippingAsync(subtotal);

        ViewBag.TotalAmount = subtotal + shipping;
        ViewBag.CartItems = items;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentIntent()
    {
        if (!TryGetUserId(out var userId)) return BadRequest();

        var items = await _orderService.GetCartItemsAsync(userId);
        if (!items.Any()) return BadRequest(new { error = "Количката е празна." });

        decimal subtotal = items.Sum(i => (i.Print?.Price ?? 0) * i.Quantity);
        decimal shipping = await _orderService.CalculateShippingAsync(subtotal);

        long amountInCents = (long)((subtotal + shipping) * 100);
        var intent = await _paymentService.CreatePaymentIntentAsync(userId, amountInCents);

        return Json(new { clientSecret = intent.ClientSecret });
    }

    [HttpGet]
    public async Task<IActionResult> Success()
    {
        if (!TryGetUserId(out var userId)) return RedirectToAction("Index", "Home");

        await _orderService.ClearCartAsync(userId);

        return View();
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out userId);
    }
}