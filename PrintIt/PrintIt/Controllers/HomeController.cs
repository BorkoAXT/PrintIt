using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Data;
using PrintIt.Models;
using PrintIt.ViewModels;
using System.Diagnostics;
using ErrorViewModel = PrintIt.ViewModels.ErrorViewModel;

namespace PrintIt.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Store()
        {
            List<Print> StoreItems = new List<Print>(_context.Prints);

            ViewData["Items"] = StoreItems;

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
