using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Models;
using PrintIt.Services;
using System.Security.Claims;

[Authorize]
public class TrackingController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<User> _userManager;

    public TrackingController(IOrderService orderService, UserManager<User> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Email == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        var orders = await _orderService.GetOrdersByEmailAsync(user.Email);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Order(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Email == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null || order.CustomerEmail != user.Email) return NotFound();

        return View(order);
    }
}
