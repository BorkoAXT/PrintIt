using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.ViewModels;
using System.Security.Claims;

namespace PrintIt.Controllers
{
    public class StoreController : BaseController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StoreController(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : base(context)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: store
        public async Task<IActionResult> Articles()
        {
            var prints = await _context.Prints.ToListAsync();
            ViewData["Items"] = prints;
            return View();
        }

        // GET: store/details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var print = await _context.Prints.FindAsync(id);
            if (print == null) return NotFound();

            bool isFavorite = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    // Проверяваме в базата дали съществува запис за този потребител и този продукт
                    isFavorite = await _context.Set<UserWishlistItem>()
                        .AnyAsync(w => w.PrintId == id && w.UserId == userId);
                }
            }

            // Използваме ViewBag, за да предадем информацията на изгледа
            ViewBag.IsFavorite = isFavorite;

            return View(print);
        }

        // GET: store/add
        [HttpGet("Store/Add")]
        [Authorize(Roles = "Admin")]
        public IActionResult Add3DPrint()
        {
            return View("~/Views/Store/Add3DPrint.cshtml", new PrintsViewModel());
        }

        // POST: store/add
        [HttpPost("Store/Add")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add3DPrint(PrintsViewModel model)
        {
            if (ModelState.IsValid)
            {
                string filePath = "/images/placeholder-figure.jpg"; // Път по подразбиране

                if (model.File != null && model.File.Length > 0)
                {
                    filePath = await UploadImage(model.File, model.PrintType);
                }

                var newPrint = new Print(model.Name, model.Description, model.MaterialType,
                    model.Weight, model.Price, filePath,
                    model.PrintType, model.PrintColors);

                _context.Prints.Add(newPrint);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Articles));
            }
            return View("~/Views/Store/Add3DPrint.cshtml", model);
        }

        // GET: store/edit/{id}
        [HttpGet("Store/Edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var print = await _context.Prints.FindAsync(id);
            if (print == null) return NotFound();

            var viewModel = new PrintsViewModel
            {
                Id = print.Id,
                Name = print.Name,
                Description = print.Description,
                Price = print.Price,
                Weight = print.Weight,
                MaterialType = print.MaterialType,
                PrintType = print.PrintType,
                PrintColors = print.PrintColors ?? new List<Enums.PrintColor>(),
                FilePath = print.FilePath
            };

            return View("~/Views/Store/Edit.cshtml", viewModel);
        }

        // POST: store/edit/{id}
        [HttpPost("Store/Edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id, PrintsViewModel model)
        {
            if (id != model.Id) return BadRequest();

            ModelState.Remove("File");

            if (ModelState.IsValid)
            {
                var existingPrint = await _context.Prints.FindAsync(id);
                if (existingPrint == null) return NotFound();

                existingPrint.Name = model.Name;
                existingPrint.Description = model.Description;
                existingPrint.Price = model.Price;
                existingPrint.Weight = model.Weight;
                existingPrint.MaterialType = model.MaterialType;
                existingPrint.PrintType = model.PrintType;
                existingPrint.PrintColors = model.PrintColors;

                if (model.File != null && model.File.Length > 0)
                {
                    // 1. Delete the physical file of the old image
                    DeleteOldImage(existingPrint.FilePath);

                    // 2. Upload the new file and get the correct relative path
                    // The helper already returns "/images/Category/filename.jpg"
                    existingPrint.FilePath = await UploadImage(model.File, model.PrintType);
                }

                _context.Update(existingPrint);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Articles));
            }
            return View(model);
        }

        // POST: store/delete/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var print = await _context.Prints.FindAsync(id);
            if (print != null)
            {
                DeleteOldImage(print.FilePath);
                _context.Prints.Remove(print);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Articles));
        }

        // Helper методи за снимките
        private async Task<string> UploadImage(IFormFile file, PrintType printType)
        {
            string categoryFolder = string.Empty;
            switch (printType)
            {
                case PrintType.Accessory: categoryFolder = "Accessories"; break;
                case PrintType.FidgetToy: categoryFolder = "FidgetToys"; break;
                case PrintType.Figure: categoryFolder = "Figures"; break;
            };

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", categoryFolder);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/images/{categoryFolder}/{uniqueFileName}";
        }

        private void DeleteOldImage(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || filePath.Contains("placeholder")) return;

            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWishlist(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userId)) return Json(new { success = false });

            var wishlistItem = await _context.Set<UserWishlistItem>()
                .FirstOrDefaultAsync(w => w.PrintId == id && w.UserId == userId);

            if (wishlistItem != null)
            {
                _context.Set<UserWishlistItem>().Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }
            else
            {
                _context.Set<UserWishlistItem>().Add(new UserWishlistItem (userId, id));
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true });
            }
        }
    }
}