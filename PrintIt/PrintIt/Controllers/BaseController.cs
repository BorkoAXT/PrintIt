using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using System.Security.Claims;

namespace PrintIt.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    var count = await _context.ShopCarts
                        .Where(c => c.UserId == userId)
                        .SelectMany(c => c.Items)
                        .CountAsync();

                    ViewData["CartCount"] = count;
                }
            }
            else
            {
                ViewData["CartCount"] = 0;
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
