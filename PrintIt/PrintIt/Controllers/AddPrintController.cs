using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrintIt.Data;
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
            return View("~/Views/Home/CreateMenu.cshtml", new PrintsViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenu(PrintsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateMenu.cshtml", model);
            }

            var newPrint = new Print
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                MaterialType = model.MaterialType,
                Weight = model.Weight
            };

            if (model.File != null && model.File.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = newPrint.Id.ToString() + Path.GetExtension(model.File.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }
            }

            _context.Add(newPrint);
            await _context.SaveChangesAsync();

            return RedirectToAction("Store", "Home");
        }
    }
}
