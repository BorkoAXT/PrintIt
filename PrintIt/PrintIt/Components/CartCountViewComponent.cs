using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PrintIt.Services;

namespace PrintIt.Components
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