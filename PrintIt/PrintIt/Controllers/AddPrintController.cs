using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Data;
using PrintIt.Models;
using PrintIt.ViewModels;

namespace PrintIt.Controllers
{
    /// <summary>
    /// Controller responsible for creating new printable products in the store.
    /// Accessible only to administrators.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AddPrintController : BaseController
    {
        /// <summary>
        /// The application database context.
        /// </summary>
        protected readonly ApplicationDbContext _context;

        /// <summary>
        /// Provides access to web hosting environment information.
        /// </summary>
        protected readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddPrintController"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="webHostEnvironment">The web hosting environment.</param>
        public AddPrintController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
            : base(context)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Displays the form for creating a new printable product.
        /// </summary>
        /// <returns>The create product view.</returns>
        [HttpGet]
        public IActionResult CreateMenu()
        {
            return View("~/Views/Store/AddStoreMenu.cshtml", new PrintsViewModel());
        }

        /// <summary>
        /// Handles the submission of a new printable product.
        /// Saves the product data and uploads the associated image file.
        /// </summary>
        /// <param name="model">The view model containing print data.</param>
        /// <returns>
        /// Redirects to the store page if creation succeeds;
        /// otherwise, returns the form view with validation errors.
        /// </returns>
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
                string figuresFolder = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "images",
                    model.PrintType.ToString());

                if (!Directory.Exists(figuresFolder))
                {
                    Directory.CreateDirectory(figuresFolder);
                }

                string fileName = newPrint.Id + Path.GetExtension(model.File.FileName);
                string fullPath = Path.Combine(figuresFolder, fileName);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }

                newPrint.FilePath = $"/images/{model.PrintType}/{fileName}";
            }

            _context.Add(newPrint);
            await _context.SaveChangesAsync();

            return RedirectToAction("Store", "Store");
        }
    }
}
