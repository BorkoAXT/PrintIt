using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace Web.Components
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;

        public CartCountViewComponent(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = 0;

            if (userId != null)
            {
            
                count = await _cartService.GetCartCountAsync(Guid.Parse(userId));
            }

            return View("~/Views/Shared/Components/Default.cshtml", count);
        }
    }
}