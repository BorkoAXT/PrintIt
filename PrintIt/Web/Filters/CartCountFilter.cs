using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Services.Interfaces;
using System.Security.Claims;

namespace Web.Filters
{
    public class CartCountFilter : IAsyncActionFilter
    {
        private readonly ICartService _cartService;

        public CartCountFilter(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var controller = context.Controller as Controller;

            if (controller != null)
            {
                int count = 0;

                var user = context.HttpContext.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    string? userIdString =
                        user.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (Guid.TryParse(userIdString, out Guid userId))
                    {
                        count = await _cartService.GetCartCountAsync(userId);
                    }
                }

                controller.ViewData["CartCount"] = count;
            }

            await next();
        }
    }
}