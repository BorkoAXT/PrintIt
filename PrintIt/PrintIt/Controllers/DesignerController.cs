using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace PrintIt.Controllers
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
