using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    public class DesignerController : Controller
    {
        [HttpGet("/Designer")]
        public async Task<IActionResult> Designer()
        {
            return View("Designer");
        }
    }
}
