using Microsoft.AspNetCore.Mvc;
using PrintIt.Data;

namespace PrintIt.Controllers
{
    public class OrderController : BaseController
    {
        public OrderController(ApplicationDbContext context) : base(context)
        {
            context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
