using Microsoft.AspNetCore.Mvc;
using PrintIt.Data;
using PrintIt.Models;

namespace PrintIt.Controllers
{
    public class StoreController : BaseController
    {
        public StoreController(ApplicationDbContext context) : base(context)
        {
        }

        [HttpGet]
        public IActionResult Store()
        {
            List<Print> StoreItems = new List<Print>(_context.Prints);

            ViewData["Items"] = StoreItems;

            return View();
        }
    }
}
