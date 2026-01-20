using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using System.Security.Claims;

namespace PrintIt.Controllers
{
    /// <summary>
    /// Base controller that provides shared functionality for all controllers in the application.
    /// </summary>
    public class BaseController : Controller
    {
        /// <summary>
        /// The application database context.
        /// </summary>
        protected readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Executes logic before and after an action method is called.
        /// Populates the shopping cart item count for the authenticated user.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        /// <param name="next">The delegate to execute the next action in the pipeline.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    int count = await _context.ShopCarts
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
