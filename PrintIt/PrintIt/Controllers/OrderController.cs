using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;

        public OrderController(ILogger<OrderController> logger, IOrderService orderService, IPaymentService paymentService)
        {
            _logger = logger;
            _orderService = orderService;
            _paymentService = paymentService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["UserId"] = userIdString;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized user attempted to access checkout.");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            try
            {
                var items = await _orderService.GetCartItemsAsync(userId);
                if (!items.Any())
                {
                    _logger.LogInformation("User {UserId} attempted checkout with an empty cart.", userId);
                    return RedirectToAction("Cart", "Cart");
                }

                decimal subtotal = await _orderService.CalculateCartTotalAsync(userId);
                decimal shipping = await _orderService.CalculateShippingAsync(subtotal);

                ViewBag.TotalAmount = subtotal + shipping;
                ViewBag.CartItems = items;
                decimal amount = ViewBag.TotalAmount;

                _logger.LogInformation("User {UserId} initiated checkout. Total: {Total}", userId, amount);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page for User {UserId}", userId);
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent()
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("CreatePaymentIntent called by unauthenticated user.");
                return BadRequest();
            }

            try
            {
                var items = await _orderService.GetCartItemsAsync(userId);
                if (!items.Any())
                {
                    _logger.LogWarning("PaymentIntent requested for empty cart by User {UserId}.", userId);
                    return BadRequest(new { error = "Количката е празна." });
                }

                decimal subtotal = items.Sum(i => (i.Print?.Price ?? 0) * i.Quantity);
                decimal shipping = await _orderService.CalculateShippingAsync(subtotal);
                long amountInCents = (long)((subtotal + shipping) * 100);

                _logger.LogInformation("Creating PaymentIntent for User {UserId} for amount {Amount} cents.", userId, amountInCents);

                var intent = await _paymentService.CreatePaymentIntentAsync(userId, amountInCents);

                _logger.LogInformation("PaymentIntent created successfully: {IntentId} for User {UserId}", intent.Id, userId);
                return Json(new { clientSecret = intent.ClientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create PaymentIntent for User {UserId}", userId);
                return StatusCode(500, new { error = "Грешка при обработка на плащането." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success()
        {
            if (!TryGetUserId(out var userId)) return RedirectToAction("Index", "Home");

            try
            {
                _logger.LogInformation("Payment success confirmed for User {UserId}. Clearing cart.", userId);
                await _orderService.ClearCartAsync(userId);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart after successful payment for User {UserId}", userId);
                return View();
            }
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdString, out userId);
        }
    }
}