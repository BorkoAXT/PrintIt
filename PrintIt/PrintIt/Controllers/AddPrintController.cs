using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Data;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.ViewModels;

namespace PrintIt.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AddPrintController : BaseController
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Base constructor for AddPrintController.
        /// </summary>
        /// <param name="context"></param>
        public AddPrintController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment) : base(context)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Action to redirect admin users to the CreateMenu view.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CreateMenu()
        {
            return View("~/Views/Store/AddStoreMenu.cshtml", new PrintsViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenu(PrintsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Store/AddStoreMenu.cshtml", model);
            }

            var newPrint = new Print(
                model.Name,
                model.Description,
                model.MaterialType,
                model.Weight,
                model.Price,
                null,
                model.PrintType,
                model.PrintColors
            );

            if (model.File != null && model.File.Length > 0)
            {
                string figuresFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", model.PrintType.ToString());

                if (!Directory.Exists(figuresFolder))
                {
                    Directory.CreateDirectory(figuresFolder);
                }

                string fileName = newPrint.Id.ToString() + Path.GetExtension(model.File.FileName);
                string fullPath = Path.Combine(figuresFolder, fileName);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }

                newPrint.FilePath = $"/images/{model.PrintType.ToString()}/" + fileName;
            }

            _context.Add(newPrint);
            await _context.SaveChangesAsync();

            return RedirectToAction("Store", "Store");
        }
    }
}
