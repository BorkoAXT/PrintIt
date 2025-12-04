using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Data;
using ErrorViewModel = PrintIt.ViewModels.ErrorViewModel;

namespace PrintIt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            //string? userEmail = HttpContext.Session.GetString("UserEmail");
            //if (userEmail is not null)
            //{
            //    return View("Store");
            //}
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
